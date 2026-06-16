using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EddnIndex.Common;
using Models = EddnIndex.Common.Models;

namespace EddnIndexUpdate;

public partial class FileProcessor
{
    private readonly Dictionary<(string? BodyName, int? BodyID, string? BodyType, string? ParentJson, long? SystemNameId, long? ModSystemAddress, decimal? X, decimal? Y, decimal? Z), List<Models.BodyInfo>> BodyCache = [];
    private readonly Dictionary<long, Models.BodyInfo> BodyCacheById = [];

    private readonly Dictionary<string, Models.BodyName> BodyNames = [];
    private readonly Dictionary<string, Models.BodyDesignation> BodyDesignations = [];
    private readonly Dictionary<(int? BodyID, string? BodyType, string? ParentJson), Models.ParentSet> ParentSets = [];

    private void Init_Bodies()
    {
        using var ctx = ContextFactory.CreateDbContext();

        if (BodyNames.Count == 0)
        {
            Logger.LogLoadingBodyNames();

            foreach (var bodyname in ctx.Set<Models.BodyName>().AsNoTracking())
            {
                BodyNames[bodyname.Name] = bodyname;
            }
        }

        if (BodyDesignations.Count == 0)
        {
            Logger.LogLoadingBodyDesignations();

            foreach (var desig in ctx.Set<Models.BodyDesignation>().AsNoTracking())
            {
                BodyDesignations[desig.Designation] = desig;
            }
        }

        if (ParentSets.Count == 0)
        {
            Logger.LogLoadingParentSets();

            foreach (var ps in ctx.Set<Models.ParentSet>().AsNoTracking())
            {
                ParentSets[(ps.BodyID, ps.BodyType, ps.ParentJson)] = ps;
            }
        }
    }

    private bool TryFillBodyDesignation(
            ReadOnlySpan<char> suffix,
            int? bodyId,
            string? bodyType,
            decimal? argOfPeriapsis,
            decimal? inclination,
            [NotNullWhen(true)] out Models.BodyDesignation? desig
        )
    {
        var suffixstr = suffix.ToString();

        desig = new Models.BodyDesignation
        {
            Designation = suffixstr,
            DesignationType = DesignationType.StellarBody,
            StarNum = 0
        };

        if (suffix.Length == 0)
        {
            return (bodyId == null || bodyId == 0)
                && argOfPeriapsis == null
                && inclination == null
                && (bodyType == null || bodyType == "Star");
        }

        if (suffix[0] != ' ') return false;
        suffix = suffix[1..];

        if (suffix.Length == 0) return false;

        var spacePos = suffix.IndexOf(' ');
        if (spacePos == -1) spacePos = suffix.Length;

        if (suffix[0] >= 'A' && suffix[0] <= 'Z' - spacePos && (suffix.Length < 6 || (!suffix[..6].SequenceEqual("Comet ") && !suffix[1..6].SequenceEqual(" Belt"))))
        {
            var star = suffix[0];

            desig = desig with
            {
                DesignationType = DesignationType.StellarBody,
                StarNum = suffix[0] - 'A' + 1
            };

            if (spacePos >= 2)
            {
                desig = desig with
                {
                    DesignationType = DesignationType.StellarBarycentre,
                    StellarBarycentreLength = spacePos
                };
            }

            for (int i = 1; i < spacePos; i++)
            {
                if (suffix[i] != star + i) return false;
            }

            suffix = suffix[spacePos..];

            if (suffix.Length == 0) return true;
            if (suffix[0] != ' ') return false;
            suffix = suffix[1..];
            spacePos = suffix.IndexOf(' ');
        }

        if (suffix.Length >= 6 && spacePos == 1 && suffix[0] >= 'A' && suffix[0] <= 'Z' && suffix[1..6].SequenceEqual(" Belt"))
        {
            desig = desig with
            {
                DesignationType = DesignationType.Belt,
                RingNum = suffix[0] - 'A' + 1
            };

            if (suffix.Length == 6)
                return true;

            if (suffix.Length >= 16 && suffix[1..15].SequenceEqual(" Belt Cluster ") && int.TryParse(suffix[15..], out int clusterNum))
            {
                desig = desig with
                {
                    DesignationType = DesignationType.AsteroidCluster,
                    ClusterNum = clusterNum
                };

                return true;
            }

            return false;
        }

        if (suffix.Length >= 7 && suffix[..6].SequenceEqual("Comet ") && int.TryParse(suffix[6..], out var cometNum))
        {
            desig = desig with
            {
                DesignationType = DesignationType.Comet,
                CometNum = cometNum
            };

            return true;
        }

        spacePos = suffix.IndexOf(' ');
        if (spacePos == -1) spacePos = suffix.Length;

        if (spacePos == 0 || suffix[0] < '1' || suffix[0] > '9') return false;
        var planet = suffix[..spacePos];
        suffix = suffix[spacePos..];
        int planetNum;

        if (planet.Contains('+'))
        {
            if (suffix.Length != 0) return false;

            var pluscount = planet.Count('+');
            Span<Range> ranges = stackalloc Range[pluscount + 1];
            planet.Split(ranges, '+', StringSplitOptions.None);
            var firstPlanet = planet[ranges[0]];
            if (!int.TryParse(firstPlanet, out var firstPlanetNum)) return false;

            desig = desig with
            {
                DesignationType = DesignationType.PlanetaryBarycentre,
                PlanetNum = firstPlanetNum,
                BarycentreLength = ranges.Length
            };

            for (int i = 1; i < ranges.Length; i++)
            {
                if (!int.TryParse(planet[ranges[i]], out planetNum) || planetNum != firstPlanetNum + i) return false;
            }

            return true;
        }

        if (!int.TryParse(planet, out planetNum)) return false;

        desig = desig with
        {
            DesignationType = DesignationType.PlanetaryBody,
            PlanetNum = planetNum
        };

        for (int moonLevel = 1; suffix.Length != 0; moonLevel++)
        {
            if (suffix[0] != ' ') return false;
            suffix = suffix[1..];

            spacePos = suffix.IndexOf(' ');
            if (spacePos == -1) spacePos = suffix.Length;

            if (suffix.Length == 6 && suffix[0] >= 'A' && suffix[0] <= 'Z' && suffix[1..].SequenceEqual(" Ring"))
            {
                desig = desig with
                {
                    DesignationType = moonLevel switch
                    {
                        1 => DesignationType.PlanetaryRing,
                        2 => DesignationType.Moon1Ring,
                        3 => DesignationType.Moon2Ring,
                        4 => DesignationType.Moon3Ring,
                        _ => DesignationType.Unknown
                    },
                    RingNum = suffix[0] - 'A' + 1
                };

                return true;
            }

            if (suffix.Length >= 7 && suffix[..6].SequenceEqual("Comet ") && int.TryParse(suffix[6..], out cometNum))
            {
                desig = desig with
                {
                    DesignationType = moonLevel switch
                    {
                        1 => DesignationType.PlanetaryComet,
                        2 => DesignationType.Moon1Comet,
                        3 => DesignationType.Moon2Comet,
                        4 => DesignationType.Moon3Comet,
                        _ => DesignationType.Unknown
                    },
                    CometNum = cometNum
                };

                return true;
            }

            var moon = suffix[..spacePos];
            suffix = suffix[spacePos..];

            if (moon.Length < 1 || moon[0] < 'a' || moon[0] > 'z') return false;

            desig = moonLevel switch
            {
                1 => desig with { DesignationType = DesignationType.Moon1Body, Moon1Num = moon[0] - 'a' + 1 },
                2 => desig with { DesignationType = DesignationType.Moon2Body, Moon2Num = moon[0] - 'a' + 1 },
                3 => desig with { DesignationType = DesignationType.Moon3Body, Moon3Num = moon[0] - 'a' + 1 },
                _ => desig with { DesignationType = DesignationType.Unknown }
            };

            if (moon.Contains('+'))
            {
                if (suffix.Length != 0) return false;
                if (moon.Length < 3 || moon.Length % 2 != 1 || moon[1] != '+') return false;

                var firstMoon = moon[0];
                if (firstMoon < 'a' || firstMoon > 'z') return false;

                desig = desig with
                {
                    DesignationType = moonLevel switch
                    {
                        1 => DesignationType.Moon1Barycentre,
                        2 => DesignationType.Moon2Barycentre,
                        3 => DesignationType.Moon3Barycentre,
                        _ => DesignationType.Unknown
                    },
                    BarycentreLength = (moon.Length - 1) / 2
                };

                for (int i = 1; i < (moon.Length - 1) / 2; i++)
                {
                    if (moon[i * 2 - 1] != '+') return false;
                    if (moon[i * 2] != firstMoon + i) return false;
                }

                return true;
            }
            else if (moon.Length != 1 || moon[0] < 'a' || moon[0] > 'z') return false;
        }

        return true;
    }

    private bool TryGetBodyDesignation(
            ReadOnlySpan<char> suffix,
            ReadOnlySpan<char> sysname,
            int? bodyId,
            string? bodyType,
            decimal? argOfPeriapsis,
            decimal? inclination,
            [NotNullWhen(true)] out Models.BodyDesignation? desig
        )
    {
        desig = null;

        if (suffix.Length < sysname.Length) return false;
        if (!suffix.StartsWith(sysname)) return false;

        suffix = suffix[sysname.Length..];

        var desigLookup = BodyDesignations.GetAlternateLookup<ReadOnlySpan<char>>();

        if (desigLookup.TryGetValue(suffix, out desig)) return true;

        if (TryFillBodyDesignation(suffix, bodyId, bodyType, argOfPeriapsis, inclination, out desig))
        {
            desig = desig with { DesignationId = desig.GetDesignationId() };

            using var ctx = ContextFactory.CreateDbContext();
            ctx.Add(desig);
            ctx.SaveChanges();

            BodyDesignations[desig.Designation] = desig;

            return true;
        }

        return false;
    }

    private int? GetOrAddBodyName(
            string? name,
            string? systemName,
            Models.SystemInfo system,
            int? bodyId,
            string? bodyType,
            decimal? argOfPeriapsis,
            decimal? inclination,
            out long? systemNameId
        )
    {
        systemNameId = null;

        if (name == null || systemName == null) return null;

        if (!BodyNameOverrides.ContainsKey(name)
            && TryGetBodyDesignation(name, systemName, bodyId, bodyType, argOfPeriapsis, inclination, out var desig))
        {
            systemNameId = system.SystemNameId;
            return desig.DesignationId ?? -desig.Id;
        }

        if (name.StartsWith(systemName) && (name.Contains("Comet") || name.Contains("Belt Cluster")))
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Logger.LogPotentialAnomalousBodyNameParsingCase(name, systemName);
        }

        if (BodyNames.TryGetValue(name, out var bodyName))
        {
            return bodyName.Id;
        }

        if (!BodyNameOverrides.ContainsKey(name))
        {
            for (var spacePos = name.LastIndexOf(' '); spacePos > 0; spacePos = name.LastIndexOf(' ', spacePos - 1))
            {
                var sysNameSpan = name.AsSpan(0, spacePos);

                if (SystemHelpers.TrySplitProcgenName(sysNameSpan, out var sectorName, out _, out _, out _)
                    && Sectors.ContainsKey(sectorName)
                    && TryGetBodyDesignation(name, sysNameSpan, bodyId, bodyType, argOfPeriapsis, inclination, out desig))
                {
                    systemNameId = GetOrAddSystemName(new string(sysNameSpan));
                    return desig.DesignationId ?? -desig.Id;
                }
            }
        }

        using var ctx = ContextFactory.CreateDbContext();

        bodyName = new Models.BodyName
        {
            Name = name
        };

        ctx.Add(bodyName);
        ctx.SaveChanges();

        BodyNames[name] = bodyName;

        return bodyName.Id;
    }

    private int? GetOrAddParentSet(int? bodyId, string? bodyType, string? parentJson)
    {
        if (bodyId == null && bodyType == null && parentJson == null) return null;

        if (parentJson != null)
        {
            parentJson = parentJson.Replace("}, {", "},{").Replace("\": ", "\":");
        }

        if (ParentSets.TryGetValue((bodyId, bodyType, parentJson), out var parentSet))
        {
            return parentSet.Id;
        }

        int? parentSetId = null;

        if (parentJson != null && parentJson.StartsWith('[') && parentJson.EndsWith(']'))
        {
            var parentEntry = parentJson[1..^1];
            string? parentParentJson = null;

            if (parentJson.Contains("},"))
            {
                var parentIndex = parentJson.IndexOf("},") + 2;
                parentParentJson = "[" + parentJson[parentIndex..].Trim();
                parentEntry = parentJson[1..(parentIndex - 1)];
            }

            if (JsonConvert.DeserializeObject<Dictionary<string, int>>(parentEntry)?.ToList() is [(string parentType, int parentBodyId)])
            {
                parentSetId = GetOrAddParentSet(parentBodyId, parentType, parentParentJson);
            }
        }

        using var ctx = ContextFactory.CreateDbContext();

        var set = new Models.ParentSet
        {
            BodyID = bodyId,
            BodyType = bodyType,
            ParentJson = parentJson,
            ParentSetId = parentSetId
        };

        ctx.Add(set);
        ctx.SaveChanges();

        ParentSets[(bodyId, bodyType, parentJson)] = set;

        return set.Id;
    }

    private static decimal DecimalRecipPow10(int scale)
    {
        if (scale < 0 || scale > 28) throw new ArgumentOutOfRangeException(nameof(scale));
        return new decimal(1, 0, 0, false, (byte)scale);
    }

    private static sbyte DecimalOrder(decimal dv, decimal error)
    {
        var log10 = (int)Math.Floor(Math.Log10((double)dv) - 0.5);

        return (sbyte)(dv * DecimalRecipPow10(log10) <= 1 - error / 2 ? log10 + 1 : log10);
    }

    private static decimal NormalizeAngle(decimal angle)
    {
        while (angle <= -180) angle += 360;
        while (angle > 180) angle -= 360;

        return angle;
    }

    private bool TryGetMatchingBody(
            List<Models.BodyInfo> bodiesList,
            decimal? argOfPeriapsis,
            decimal? inclination,
            decimal? semiMajorAxis,
            [NotNullWhen(true)] out Models.BodyInfo? body,
            out short? semiMajorAxisError,
            out short? inclinationError,
            out short? argOfPeriapsisError
        )
    {
        body = null;
        argOfPeriapsisError = null;
        inclinationError = null;
        semiMajorAxisError = null;

        foreach (var (item, smadiff, aopdiff, incdiff) in bodiesList
                                       .Where(e => e.SemiMajorAxis.HasValue == semiMajorAxis.HasValue
                                                && e.ArgOfPeriapsis.HasValue == argOfPeriapsis.HasValue
                                                && e.Inclination.HasValue == inclination.HasValue)
                                       .Select(e => (
                                            Body: e,
                                            SMADiff: (semiMajorAxis ?? 0) * DecimalRecipPow10(e.SemiMajorAxisScale) - (e.SemiMajorAxis ?? 0),
                                            AOPDiff: NormalizeAngle((argOfPeriapsis ?? 0) - (e.ArgOfPeriapsis ?? 0)),
                                            IncDiff: NormalizeAngle((inclination ?? 0) - (e.Inclination ?? 0))
                                       ))
                                       .Where(e => e is
                                       {
                                            SMADiff: > -0.001m and < 0.001m,
                                            AOPDiff: > -0.001m and < 0.001m,
                                            IncDiff: > -0.001m and < 0.001m
                                       }))
        {

            semiMajorAxisError = (short)Math.Round(smadiff * 1000000);
            argOfPeriapsisError = (short)Math.Round(aopdiff * 1000000);
            inclinationError = (short)Math.Round(incdiff * 1000000);

            Assert(body == null, extraData: bodiesList);
            Assert(incdiff > -0.001m
                && incdiff < 0.001m
                && aopdiff > -0.001m
                && aopdiff < 0.001m
                && smadiff > -0.001m
                && smadiff < 0.001m,
                extraData: new
                {
                    Current = new
                    {
                        item.SemiMajorAxis,
                        item.SemiMajorAxisScale,
                        item.ArgOfPeriapsis,
                        item.Inclination
                    },
                    Updated = new
                    {
                        semiMajorAxis,
                        argOfPeriapsis,
                        inclination
                    },
                    smadiff,
                    aopdiff,
                    incdiff
                }
            );

            body = item;
        }

        return body != null;
    }

    private (Models.BodyInfo body, short? smaerror, short? aoperror, short? incerror) GetOrAddBody(
            string? name,
            string? systemName,
            int? bodyId,
            string? bodyType,
            string? parentJson,
            decimal? argOfPeriapsis,
            decimal? inclination,
            decimal? semiMajorAxis,
            DateTime? timestamp,
            string? gameVersion,
            Models.SystemInfo system
        )
    {
        if (argOfPeriapsis == null || inclination == null || semiMajorAxis == null || semiMajorAxis <= 0)
        {
            argOfPeriapsis = null;
            inclination = null;
            semiMajorAxis = null;
        }

        if (!BodyCache.TryGetValue((name, bodyId, bodyType, parentJson, system.SystemNameId, system.ModSystemAddress, system.X, system.Y, system.Z), out var bodyList))
        {
            BodyCache[(name, bodyId, bodyType, parentJson, system.SystemNameId, system.ModSystemAddress, system.X, system.Y, system.Z)] = bodyList = [];
        }

        if (TryGetMatchingBody(bodyList, argOfPeriapsis, inclination, semiMajorAxis, out var body, out var smaerror, out var incerror, out var aoperror))
        {
            return (body, smaerror, aoperror, incerror);
        }

        var bodyNameId = GetOrAddBodyName(name, systemName, system, bodyId, bodyType, argOfPeriapsis, inclination, out var sysNameId);
        var parentSetId = GetOrAddParentSet(bodyId, bodyType, parentJson);

        if (system.Id != 0 && bodyList.Count == 0)
        {
            using var ctx = ContextFactory.CreateDbContext();

            bodyList.AddRange(
                ctx.Set<Models.BodyInfo>()
                   .Where(e =>
                        e.SystemId == system.Id
                        && e.BodyNameId == bodyNameId
                        && e.SystemNameId == sysNameId
                        && e.ParentSetId == parentSetId
                   )
                   .AsEnumerable()
                   .Select(e =>
                   {
                       if (!BodyCacheById.TryGetValue(e.Id, out var byid))
                       {
                           BodyCacheById[e.Id] = byid = e;
                       }

                       return byid;
                   })
            );

            if (TryGetMatchingBody(bodyList, argOfPeriapsis, inclination, semiMajorAxis, out body, out smaerror, out incerror, out aoperror))
            {
                return (body, smaerror, incerror, aoperror);
            }
        }

        int? bodyDesigId = null;

        if (sysNameId == system.SystemNameId && bodyNameId < 0)
        {
            bodyDesigId = bodyNameId;
        }
        else if (sysNameId == null
                 && system.ModSystemAddress == system.NameModSystemAddress
                 && name != null
                 && BodyNameOverrides.TryGetValue(name, out var overrides))
        {
            overrides = [..
                overrides
                    .Where(e => e.SystemName == systemName
                                && e.ArgOfPeriapsisEquals(argOfPeriapsis) != false
                                && e.InclinationEquals(inclination) != false
                                && (bodyId == null || e.BodyID == bodyId))
            ];

            if (system.SystemAddress != null && overrides.Any(e => e.SystemAddress == system.SystemAddress))
            {
                overrides = [.. overrides.Where(e => e.SystemAddress == system.SystemAddress)];
            }

            if (gameVersion?.StartsWith('4') == false && overrides.Any(e => e.SinceVersion?.StartsWith('4') != true))
            {
                overrides = [.. overrides.Where(e => e.SinceVersion?.StartsWith('4') != true)];
            }

            if (overrides.Any(e => e.ValidFrom < timestamp && e.ValidTo > timestamp))
            {
                overrides = [.. overrides.Where(e => e.ValidFrom < timestamp && e.ValidTo > timestamp)];
            }

            if (bodyType != null && overrides.Any(e => e.BodyType == bodyType))
            {
                overrides = [.. overrides.Where(e => e.BodyType == bodyType)];
            }

            if (overrides.Count > 1)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                Logger.LogMultipleBodyDesignationOverridesMatched(systemName, bodyId, bodyType, timestamp, overrides.Count);
            }

            if (overrides is [{ } ovr] && TryGetBodyDesignation(ovr.BodyDesignation, systemName, bodyId, bodyType, argOfPeriapsis, inclination, out var desig))
            {
                bodyDesigId = desig.Id;
            }
        }

        var smascale = (sbyte)(semiMajorAxis == null || semiMajorAxis < 10 ? 0 : Math.Floor(Math.Log10((double)semiMajorAxis) - 0.5));
        var sma = semiMajorAxis * DecimalRecipPow10(smascale);

        if (sma > 10)
        {
            smascale++;
            sma *= 0.1m;
        }

        body = new Models.BodyInfo
        {
            BodyNameId = bodyNameId,
            BodyDesignationId = bodyDesigId,
            BodyId = bodyId,
            SystemNameId = sysNameId,
            System = system,
            ParentSetId = parentSetId,
            ArgOfPeriapsis = argOfPeriapsis,
            Inclination = inclination,
            SemiMajorAxis = sma,
            SemiMajorAxisScale = smascale
        };

        bodyList.Add(body);

        return (body, null, null, null);
    }
}
