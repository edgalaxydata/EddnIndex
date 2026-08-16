using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EddnIndex.Common;
using Models = EddnIndex.Common.Models;
using EddnIndex.Common.Sectors;

namespace EddnIndexUpdate;

public partial class FileProcessor
{
    private readonly Dictionary<(string? SystemName, long? SystemAddress, decimal? X, decimal? Y, decimal? Z), Models.SystemInfo> SystemCache = [];
    private readonly Dictionary<int, Models.SystemInfo> SystemCacheById = [];

    private readonly Dictionary<string, Models.SystemName> SystemNames = [];
    private readonly Dictionary<int, Models.SystemName> SystemNamesById = [];
    private readonly Dictionary<string, Models.Sector> Sectors = [];
    private readonly Dictionary<int, Models.Sector> SectorsById = [];
    private readonly Dictionary<int, Models.Sector> SectorsByAddr = [];

    private async Task Init_SystemsAsync(CancellationToken canceltoken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        if (Sectors.Count == 0 || SectorsById.Count == 0)
        {
            Logger.LogLoadingSectors();

            foreach (var sector in ctx.Set<Models.Sector>().AsNoTracking())
            {
                Sectors[sector.Name] = sector;
            }

            foreach (var hagrp in HandAuthoredSectors.Sectors.GroupBy(e => (e.Name, e.X0, e.Y0, e.Z0, e.ValidFrom, e.ValidTo)))
            {
                var (name, x0, y0, z0, validFrom, validTo) = hagrp.Key;
                var sizeX = hagrp.Max(e => e.X + e.Radius) - x0;
                var sizeY = hagrp.Max(e => e.Y + e.Radius) - y0;
                var sizeZ = hagrp.Max(e => e.Z + e.Radius) - z0;
                var haSectorPriority = (int)hagrp.Min(e => e.Id);

                if (!Sectors.ContainsKey(name))
                {
                    var sector = new Models.Sector
                    {
                        Name = name,
                        X0 = x0,
                        Y0 = y0,
                        Z0 = z0,
                        SizeX = sizeX,
                        SizeY = sizeY,
                        SizeZ = sizeZ,
                        HASectorPriority = haSectorPriority,
                        IsHASector = true,
                        ValidFrom = validFrom,
                        ValidTo = validTo
                    };

                    ctx.Add(sector);

                    Sectors[sector.Name] = sector;
                }
            }

            var gotSectors = new HashSet<(int X, int Y, int Z)>();

            for (int r = 0; r < 36; r++)
            {
                int ymax = r > 26 ? 2 : r > 16 ? 3 : 4;

                for (int y0 = 0; y0 < ymax; y0++)
                {
                    for (int oct = 0; oct < 16; oct++)
                    {
                        int xm = (oct & 2) == 0 ? 1 : -1;
                        int xo = (oct & 2) == 0 ? 0 : -1;
                        int ym = (oct & 8) == 0 ? 1 : -1;
                        int yo = (oct & 8) == 0 ? 0 : -1;
                        int zm = (oct & 4) == 0 ? 1 : -1;
                        int zo = (oct & 4) == 0 ? 0 : -1;
                        bool swapxz = (oct & 1) != 0;

                        for (int x0 = 0; x0 <= r; x0++)
                        {
                            for (int z0 = 0; z0 <= r; z0++)
                            {
                                var x = x0 * xm + xo + 39;
                                var y = y0 * ym + yo + 32;
                                var z = z0 * zm + zo + 39;

                                if (swapxz)
                                {
                                    (x, z) = (z, x);
                                }

                                if (!gotSectors.Contains((x, y, z)))
                                {
                                    var sectorAddress = x + y * 128 + z * 8192;
                                    var sectorName = PGSectors.GetSectorName(sectorAddress);

                                    if (!Sectors.ContainsKey(sectorName))
                                    {
                                        var sector = new Models.Sector
                                        {
                                            Name = sectorName,
                                            X0 = x * 1280 - 49985,
                                            Y0 = y * 1280 - 40985,
                                            Z0 = z * 1280 - 24105,
                                            IsHASector = false,
                                            SectorAddress = sectorAddress,
                                            SizeX = 1280,
                                            SizeY = 1280,
                                            SizeZ = 1280,
                                            ValidFrom = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                            ValidTo = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
                                        };

                                        ctx.Add(sector);
                                        Sectors[sectorName] = sector;
                                    }

                                    gotSectors.Add((x, y, z));
                                }
                            }
                        }
                    }
                }
            }

            await ctx.SaveChangesAsync(canceltoken);

            foreach (var sector in Sectors.Values)
            {
                SectorsById[sector.Id] = sector;

                if (sector.SectorAddress is int sectorAddr)
                {
                    SectorsByAddr[sectorAddr] = sector;
                }
            }
        }

        if (SystemNames.Count == 0 || SystemNamesById.Count == 0)
        {
            foreach (var sysname in ctx.Set<Models.SystemName>().AsNoTracking())
            {
                SystemNames[sysname.Name] = sysname;
                SystemNamesById[sysname.Id] = sysname;
            }
        }
    }

    private async Task<Models.Sector> GetOrAddSectorAsync(string name, CancellationToken canceltoken)
    {
        if (Sectors.TryGetValue(name, out var sector)) return sector;

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        sector = new Models.Sector
        {
            Name = name
        };

        if (PGSectors.TryGetSectorId(name, out int sectorid) && PGSectors.GetSectorName(sectorid) == name)
        {
            sector = sector with
            {
                SectorAddress = sectorid,
                SizeX = 1280,
                SizeY = 1280,
                SizeZ = 1280,
                X0 = (sectorid & 0x7F) * 1280 - 49985,
                Y0 = ((sectorid >> 7) & 0x3F) * 1280 - 40985,
                Z0 = ((sectorid >> 13) & 0x7F) * 1280 - 24105,
                IsHASector = false
            };
        }

        ctx.Add(sector);
        await ctx.SaveChangesAsync(canceltoken);
        Sectors[name] = sector;
        return sector;
    }

    [return: NotNullIfNotNull(nameof(name))]
    private async Task<long?> GetOrAddSystemNameAsync(string? name, CancellationToken canceltoken)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        if (SystemHelpers.TrySplitProcgenName(name, out var sectorName, out var mid, out var n2, out var masscode)
            && n2 >= 0
            && n2 < 65536
            && mid >= 0
            && mid < 0x200000
            && masscode >= 0
            && masscode < 8)
        {
            var boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);
            var checkSuffix = SystemHelpers.GetPGSuffix(boxelid);
            Assert(name.EndsWith(checkSuffix), extraData: new { name, checkSuffix });

            var sector = await GetOrAddSectorAsync(sectorName, canceltoken);

            if (sector.SectorAddress is int sectoraddr && sectoraddr >= 0 && sectoraddr < 0x100000)
            {
                return (long)sectoraddr << 40 | boxelid;
            }

            return ((long)sector.Id + 0x100000) << 40 | boxelid;
        }

        if (SystemNames.TryGetValue(name, out var systemname))
        {
            return -systemname.Id;
        }

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);
        systemname = new Models.SystemName { Name = name };
        ctx.Add(systemname);
        await ctx.SaveChangesAsync(canceltoken);

        SystemNames[name] = systemname;
        SystemNamesById[systemname.Id] = systemname;

        return -systemname.Id;
    }

    private long? TryGetNameModSystemAddress(long? nameid)
    {
        if (nameid is not long nameId) return null;

        if (nameId >= 0 && nameId < 0x1000_0000_0000_0000)
        {
            return nameId;
        }
        else if (nameId >= 0x1000_0000_0000_0000)
        {
            var n2 = nameId & 0xFFFF;
            var mid = (nameId >> 16) & 0x1FFFFF;
            var masscode = (int)((nameId >> 37) & 7);
            var sectorid = (int)((nameId >> 40) - 0x100000);

            if (!SectorsById.TryGetValue(sectorid, out var sector)
                || sector.X0 == null
                || sector.Y0 == null
                || sector.Z0 == null)
            {
                return null;
            }

            var x0 = (int)((sector.X0 + 49985) / (10 << masscode));
            var y0 = (int)((sector.Y0 + 40985) / (10 << masscode));
            var z0 = (int)((sector.Z0 + 24105) / (10 << masscode));
            var xv = (mid & 0x7F) + x0;
            var yv = ((mid >> 7) & 0x7F) + y0;
            var zv = ((mid >> 14) & 0x7F) + z0;
            mid = (xv & (0x7F >> masscode)) | ((yv & (0x7F >> masscode)) << 7) | ((zv & (0x7F >> masscode)) << 14);
            var sectorAddr = (xv >> (7 - masscode)) | ((yv >> (7 - masscode)) << 7) | ((zv >> (7 - masscode)) << 13);
            return n2 | (mid << 16) | ((long)masscode << 37) | (sectorAddr << 40);
        }

        return null;
    }

    private static decimal? RoundCoords(decimal? v)
    {
        if (v is not decimal val) return null;
        return Math.Round(val * 32) / 32;
    }

    private async Task<Models.SystemInfo> GetOrAddSystemAsync(
            string? name,
            long? systemAddress,
            decimal? x,
            decimal? y,
            decimal? z,
            CancellationToken canceltoken
        )
    {
        x = RoundCoords(x);
        y = RoundCoords(y);
        z = RoundCoords(z);

        if (x <= -100000 || x >= 100000 || y <= -100000 || y >= 100000 || z <= -100000 || z >= 100000)
        {
            x = y = z = null;
        }

        if (SystemCache.TryGetValue((name, systemAddress, x, y, z), out var system))
        {
            return system;
        }

        var nameid = await GetOrAddSystemNameAsync(name, canceltoken);
        var modsysaddr = SystemHelpers.SystemAddressToModSystemAddress(systemAddress);
        var revsysaddr = SystemHelpers.ModSystemAddressToSystemAddress(modsysaddr);
        Assert(systemAddress == revsysaddr, extraData: new { modsysaddr, systemAddress, revsysaddr });
        var namemodsysaddr = TryGetNameModSystemAddress(nameid);

        DateTime? validFrom = null;
        DateTime? validTo = null;

        if (name != null && SystemNameOverrides.TryGetValue(name, out var overrides))
        {
            if (overrides.Count > 1
                && x != null
                && y != null
                && z != null
                && overrides.Any(e => e.X == x && e.Y == y && e.Z == z))
            {
                overrides = [.. overrides.Where(e => e.X == x && e.Y == y && e.Z == z)];
            }

            if (overrides.Count > 1
                && systemAddress != null
                && overrides.Any(e => e.SystemAddress == systemAddress))
            {
                overrides = [.. overrides.Where(e => e.SystemAddress == systemAddress)];
            }

            if (overrides.Count > 1 && (systemAddress != null || x != null || y != null || z != null))
            {
                Debugger.Break();
            }

            if (overrides is [{ } ovr])
            {
                namemodsysaddr ??= SystemHelpers.SystemAddressToModSystemAddress(ovr.SystemAddress);

                if ((systemAddress == null || ovr.SystemAddress == systemAddress)
                    && (ovr.X == null || x == null || ovr.X == x)
                    && (ovr.Y == null || y == null || ovr.Y == y)
                    && (ovr.Z == null || z == null || ovr.Z == z))
                {
                    validFrom = ovr.ValidFrom;
                    validTo = ovr.ValidTo;
                }
            }
        }

        modsysaddr ??= namemodsysaddr;

        var namesysaddr = SystemHelpers.ModSystemAddressToSystemAddress(namemodsysaddr);
        var revnamemodsysaddr = SystemHelpers.SystemAddressToModSystemAddress(namesysaddr);
        Assert(namemodsysaddr == revnamemodsysaddr, extraData: new { namemodsysaddr, namesysaddr, revnamemodsysaddr });

        systemAddress ??= namesysaddr;

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        system = await
            ctx.Set<Models.SystemInfo>()
               .AsNoTracking()
               .FirstOrDefaultAsync(
                    e => e.SystemNameId == nameid
                      && e.ModSystemAddress == modsysaddr
                      && e.X == x
                      && e.Y == y
                      && e.Z == z,
                    canceltoken
               );

        if (system != null)
        {
            if (!SystemCacheById.TryGetValue(system.Id, out var byid))
            {
                SystemCacheById[system.Id] = byid = system;
            }

            system = byid;
        }
        else
        {
            system = new Models.SystemInfo
            {
                SystemNameId = nameid,
                ModSystemAddress = modsysaddr,
                NameModSystemAddress = namemodsysaddr,
                X = x,
                Y = y,
                Z = z,
                ValidFrom = validFrom ?? new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = validTo ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            };
        }

        SystemCache.Add((name, systemAddress, x, y, z), system);

        if (systemAddress != null && modsysaddr == namemodsysaddr)
        {
            SystemCache.Add((name, null, x, y, z), system);
        }

        return system;
    }
}
