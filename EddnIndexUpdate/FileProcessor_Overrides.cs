using Csv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace EddnIndexUpdate;

public partial class FileProcessor
{
    private readonly Dictionary<string, List<Models.BodyNameOverride>> BodyNameOverrides = [];
    private readonly Dictionary<string, List<Models.SystemNameOverride>> SystemNameOverrides = [];
    private readonly Dictionary<string, Models.GameVersionDate> GameVersionDates = [];
    private readonly Dictionary<string, Models.FilePrefixSchema> SchemasByFilePrefix = [];

    private string BodyOverridesFile => Path.IsPathRooted(Settings.BodyOverridesFile)
                                      ? Settings.BodyOverridesFile
                                      : Path.Combine(Settings.BaseDir, Settings.BodyOverridesFile);

    private string SystemOverridesFile => Path.IsPathRooted(Settings.SystemOverridesFile)
                                        ? Settings.SystemOverridesFile
                                        : Path.Combine(Settings.BaseDir, Settings.SystemOverridesFile);

    private string GameVersionDatesFile => Path.IsPathRooted(Settings.GameVersionDatesFile)
                                         ? Settings.GameVersionDatesFile
                                         : Path.Combine(Settings.BaseDir, Settings.GameVersionDatesFile);

    private string MessageTypesFile => Path.IsPathRooted(Settings.MessageTypesFile)
                                     ? Settings.MessageTypesFile
                                     : Path.Combine(Settings.BaseDir, Settings.MessageTypesFile);

    private void Init_Overrides()
    {
        using var ctx = ContextFactory.CreateDbContext();

        if (SchemasByFilePrefix.Count == 0)
        {
            Logger.LogInformation("Loading message types");

            foreach (var schema in ctx.Set<Models.FilePrefixSchema>().AsNoTracking())
            {
                SchemasByFilePrefix[schema.FilenamePrefix] = schema;
            }

            if (File.Exists(MessageTypesFile))
            {
                Logger.LogInformation("Process message types file");

                foreach (var line in File.ReadLines(MessageTypesFile))
                {
                    if (line.Trim().Split('\t') is [string schema, string prefix] && !SchemasByFilePrefix.ContainsKey(prefix))
                    {
                        string? eventType = null;

                        if (prefix.Split('.', 2) is ["Journal" or "Test-Journal", string type])
                        {
                            eventType = type;
                        }

                        var info = new Models.FilePrefixSchema
                        {
                            FilenamePrefix = prefix,
                            PrimarySchema = schema,
                            EventType = eventType,
                            IsTest = prefix.StartsWith("Test-")
                        };

                        ctx.Add(info);

                        SchemasByFilePrefix[prefix] = info;
                    }
                }

                ctx.SaveChanges();
                ctx.ChangeTracker.Clear();
            }
        }

        if (GameVersionDates.Count == 0)
        {
            Logger.LogInformation("Loading game version dates");

            foreach (var ent in ctx.Set<Models.GameVersionDate>())
            {
                GameVersionDates[ent.Version] = ent;
            }

            if (!File.Exists(GameVersionDatesFile))
            {
                Logger.LogInformation("Retrieving game version dates");

                DownloadGameVersions(GameVersionDatesFile);
            }

            if (File.Exists(GameVersionDatesFile))
            {
                foreach (var line in File.ReadLines(GameVersionDatesFile))
                {
                    if (JsonConvert.DeserializeObject<Models.GameVersionDate>(line) is { } ent)
                    {
                        if (!GameVersionDates.TryGetValue(ent.Version, out var curver))
                        {
                            GameVersionDates[ent.Version] = ent;
                            ctx.Add(ent);
                        }
                        else
                        {
                            curver.UpdateStartTime ??= ent.UpdateStartTime;
                            curver.UpdateEndTime ??= ent.UpdateEndTime;
                        }
                    }
                }
            }

            ctx.SaveChanges();
            ctx.ChangeTracker.Clear();
        }

        if (SystemNameOverrides.Count == 0)
        {
            Logger.LogInformation("Loading system name overrides");

            foreach (var ent in ctx.Set<Models.SystemNameOverride>().AsNoTracking())
            {
                if (!SystemNameOverrides.TryGetValue(ent.Name, out var overrides))
                {
                    SystemNameOverrides[ent.Name] = overrides = [];
                }

                overrides.Add(ent);
            }

            if (!File.Exists(SystemOverridesFile))
            {
                Logger.LogInformation("Retrieving system name overrides");

                DownloadSystemNameOverrides(SystemOverridesFile);
            }

            if (File.Exists(SystemOverridesFile))
            {
                foreach (var line in File.ReadLines(SystemOverridesFile))
                {
                    if (JsonConvert.DeserializeObject<Models.SystemNameOverride>(line) is { } ent)
                    {
                        if (!SystemNameOverrides.TryGetValue(ent.Name, out var overrides))
                        {
                            SystemNameOverrides[ent.Name] = overrides = [];
                        }

                        if (!overrides.Any(e => e.SystemAddress == ent.SystemAddress
                                             && (e.X == null || ent.X == null || e.X == ent.X)
                                             && (e.Y == null || ent.Y == null || e.Y == ent.Y)
                                             && (e.Z == null || ent.Z == null || e.Z == ent.Z)
                                             && (e.ValidFrom == null || ent.ValidFrom == null || e.ValidFrom == ent.ValidFrom)
                                             && (e.ValidTo == null || ent.ValidTo == null || e.ValidTo == ent.ValidTo)))
                        {
                            overrides.Add(ent);
                            ctx.Add(ent);
                        }
                    }
                }
            }

            ctx.SaveChanges();
            ctx.ChangeTracker.Clear();
        }

        if (BodyNameOverrides.Count == 0)
        {
            Logger.LogInformation("Loading body name overrides");

            var bysysaddr = new Dictionary<long, List<Models.BodyNameOverride>>();

            foreach (var ent in ctx.Set<Models.BodyNameOverride>().AsNoTracking())
            {
                if (!BodyNameOverrides.TryGetValue(ent.BodyName, out var overrides))
                {
                    BodyNameOverrides[ent.BodyName] = overrides = [];
                }

                if (!bysysaddr.TryGetValue(ent.SystemAddress, out var sysov))
                {
                    bysysaddr[ent.SystemAddress] = sysov = [];
                }

                overrides.Add(ent);
                sysov.Add(ent);
            }

            if (!File.Exists(BodyOverridesFile))
            {
                Logger.LogInformation("Retrieving body overrides");
                DownloadBodyNameOverrides(BodyOverridesFile);
            }

            if (File.Exists(BodyOverridesFile))
            {
                foreach (var line in File.ReadLines(BodyOverridesFile))
                {
                    if (JsonConvert.DeserializeObject<Models.BodyNameOverride>(line) is { } ent)
                    {
                        ent = ent with
                        {
                            ValidFrom = string.IsNullOrWhiteSpace(ent.SinceVersion)
                                     || !GameVersionDates.TryGetValue(ent.SinceVersion, out var ver)
                                      ? new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                      : ver.UpdateTime,
                            ValidTo   = string.IsNullOrWhiteSpace(ent.UntilVersion)
                                     || !GameVersionDates.TryGetValue(ent.UntilVersion, out ver)
                                      ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
                                      : ver.UpdateTime
                        };

                        if (!BodyNameOverrides.TryGetValue(ent.BodyName, out var overrides))
                        {
                            BodyNameOverrides[ent.BodyName] = overrides = [];
                        }

                        if (!bysysaddr.TryGetValue(ent.SystemAddress, out var sysov))
                        {
                            bysysaddr[ent.SystemAddress] = sysov = [];
                        }

                        if (!overrides.Any(e => e.SystemAddress == ent.SystemAddress
                                             && e.SystemName == ent.SystemName
                                             && e.BodyID == ent.BodyID
                                             && e.BodyDesignation == ent.BodyDesignation
                                             && e.BodyType == ent.BodyType
                                             && e.ArgOfPeriapsis == ent.ArgOfPeriapsis
                                             && e.Inclination == ent.Inclination))
                        {
                            ctx.Add(ent);
                            overrides.Add(ent);
                            sysov.Add(ent);
                        }
                    }
                }
            }

            foreach (var (name, overrides) in BodyNameOverrides)
            {
                if (SystemNameOverrides.TryGetValue(name, out var sysoverrides))
                {
                    foreach (var sysov in sysoverrides)
                    {
                        if (bysysaddr.GetValueOrDefault(sysov.SystemAddress)?.Any(e => e.SystemName == sysov.Name) != true)
                        {
                            var ent = new Models.BodyNameOverride
                            {
                                BodyName = name,
                                SystemName = name,
                                SystemAddress = sysov.SystemAddress,
                                BodyDesignation = name,
                                BodyID = 0,
                                BodyType = "Star",
                                ValidFrom = sysov.ValidFrom,
                                ValidTo = sysov.ValidTo
                            };

                            overrides.Add(ent);
                        }
                    }
                }
            }

            ctx.SaveChanges();
            ctx.ChangeTracker.Clear();
        }
    }

    private static string? GetCsvField(ICsvLine line, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "-") return null;
        if (!line.HasColumn(name)) return null;
        return line[name];
    }

    private void DownloadBodyNameOverrides(string filename)
    {
        var settings = Settings.BodyOverridesCsv;

        var byName = new Dictionary<string, List<Models.BodyNameOverride>>();

        if (!string.IsNullOrWhiteSpace(settings.URI))
        {
            using var stream = HttpClient.GetStreamAsync(settings.URI).Result;
            ProcessBodyNameOverridesCsv(byName, stream);
        }

        if (!string.IsNullOrWhiteSpace(settings.Filename))
        {
            using var stream = File.Open(settings.Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            ProcessBodyNameOverridesCsv(byName, stream);
        }

        if (byName.Count != 0)
        {
            using (var outfile = File.Open(filename + ".tmp", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using var writer = new StreamWriter(outfile, new UTF8Encoding(false));

                foreach (var overrides in byName.Values.Where(e => e.Any(o => o.BodyName != o.BodyDesignation)))
                {
                    foreach (var ent in overrides)
                    {
                        writer.WriteLine(JsonConvert.SerializeObject(ent, Formatting.None));
                    }
                }
            }

            File.Move(filename + ".tmp", filename, true);
        }
    }

    private void ProcessBodyNameOverridesCsv(Dictionary<string, List<Models.BodyNameOverride>> byName, Stream stream)
    {
        var fields = Settings.BodyOverridesCsv.Fields;

        foreach (var line in CsvReader.ReadFromStream(stream))
        {
            var sysName = GetCsvField(line, fields.SystemName);
            var bodyDesig = GetCsvField(line, fields.BodyDesignation);
            var bodyName = GetCsvField(line, fields.BodyName);

            if (long.TryParse(GetCsvField(line, fields.SystemAddress), out var sysaddr)
                && int.TryParse(GetCsvField(line, fields.BodyID), out var bodyId)
                && sysName != null
                && bodyDesig != null
                && bodyName != null
                && (bodyName == sysName || bodyDesig != bodyName))
            {
                var sinceVersion = GetCsvField(line, fields.SinceVersion);
                var untilVersion = GetCsvField(line, fields.UntilVersion);
                var isStar = GetCsvField(line, fields.IsStar);
                var argOfPeriapsis = decimal.TryParse(GetCsvField(line, fields.ArgOfPeriapsis), out var dv) ? dv : (decimal?)null;
                var inclination = decimal.TryParse(GetCsvField(line, fields.Inclination), out dv) ? dv : (decimal?)null;

                if (!Enum.TryParse<BodyType>(GetCsvField(line, fields.BodyType), out var bodyType))
                {
                    bodyType = isStar switch
                    {
                        "Y" => BodyType.Star,
                        "R" when bodyDesig.EndsWith(" Belt") => BodyType.StellarRing,
                        "R" => BodyType.PlanetaryRing,
                        "N" => BodyType.Planet,
                        "BC" => BodyType.AsteroidCluster,
                        "C" => BodyType.SmallBody,
                        _ => BodyType.None
                    };
                }

                if (!byName.TryGetValue(bodyName, out var overrides))
                {
                    byName[bodyName] = overrides = [];
                }

                overrides.Add(new Models.BodyNameOverride
                {
                    SystemAddress = sysaddr,
                    SystemName = sysName,
                    BodyID = bodyId,
                    BodyName = bodyName,
                    BodyDesignation = bodyDesig,
                    ArgOfPeriapsis = argOfPeriapsis,
                    Inclination = inclination,
                    SinceVersion = sinceVersion,
                    UntilVersion = untilVersion,
                    BodyType = bodyType == BodyType.None ? null : bodyType.ToString()
                });
            }
        }
    }

    private void DownloadSystemNameOverrides(string filename)
    {
        var sysOverrides = new Dictionary<long, List<Models.SystemNameOverride>>();
        var overridesJsonSettings = Settings.SystemOverridesJson;
        var overridesCsvSettings = Settings.SystemOverridesCsv;
        var renamesCsvSettings = Settings.SystemRenamesCsv;

        if (!string.IsNullOrWhiteSpace(overridesJsonSettings.URI))
        {
            var systemsJson = HttpClient.GetStringAsync(overridesJsonSettings.URI).Result;
            ProcessSystemOverridesJson(sysOverrides, systemsJson);
        }

        if (!string.IsNullOrWhiteSpace(overridesJsonSettings.Filename))
        {
            var systemsJson = File.ReadAllText(overridesJsonSettings.Filename);
            ProcessSystemOverridesJson(sysOverrides, systemsJson);
        }

        if (!string.IsNullOrWhiteSpace(overridesCsvSettings.URI))
        {
            var systemsCsv = HttpClient.GetStringAsync(overridesCsvSettings.URI).Result;
            ProcessSystemOverridesCsv(sysOverrides, systemsCsv);
        }

        if (!string.IsNullOrWhiteSpace(overridesCsvSettings.Filename))
        {
            var systemsCsv = File.ReadAllText(overridesCsvSettings.Filename);
            ProcessSystemOverridesCsv(sysOverrides, systemsCsv);
        }

        if (!string.IsNullOrWhiteSpace(renamesCsvSettings.URI))
        {
            var renamesCsv = HttpClient.GetStringAsync(renamesCsvSettings.URI).Result;
            ProcessSystemRenamesCsv(sysOverrides, renamesCsv);
        }

        if (!string.IsNullOrWhiteSpace(renamesCsvSettings.Filename))
        {
            var renamesCsv = File.ReadAllText(renamesCsvSettings.Filename);
            ProcessSystemRenamesCsv(sysOverrides, renamesCsv);
        }

        if (sysOverrides.Count != 0)
        {
            using (var outfile = File.Open(filename + ".tmp", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using var writer = new StreamWriter(outfile, new UTF8Encoding(false));

                foreach (var syslist in sysOverrides.Values)
                {
                    foreach (var ent in syslist)
                    {
                        writer.WriteLine(JsonConvert.SerializeObject(ent, Formatting.None));
                    }
                }
            }

            File.Move(filename + ".tmp", filename, true);
        }
    }

    private void ProcessSystemRenamesCsv(Dictionary<long, List<Models.SystemNameOverride>> sysOverrides, string renamesCsv)
    {
        var fields = Settings.SystemRenamesCsv.Fields;

        foreach (var line in CsvReader.ReadFromText(renamesCsv))
        {
            if (GetCsvField(line, fields.PreviousSystemName) is string prevname
                && GetCsvField(line, fields.SystemName) is string sysname
                && long.TryParse(GetCsvField(line, fields.SystemAddress), out var sysaddr)
                && DateTime.TryParse(GetCsvField(line, fields.RenameDate), out var date))
            {
                date = date.AddHours(10);

                if (!sysOverrides.TryGetValue(sysaddr, out var syslist))
                {
                    sysOverrides[sysaddr] = syslist = [];
                }

                var prev = syslist.FirstOrDefault(e => e.Name == prevname && (e.ValidTo == null || e.ValidTo == date));
                var next = syslist.FirstOrDefault(e => e.Name == sysname && (e.ValidFrom == null || e.ValidFrom == date));

                if (prev != null)
                {
                    syslist.Remove(prev);
                    syslist.Add(prev with { ValidTo = date });
                }
                else if (!Models.SystemInfo.TrySplitProcgenName(sysname, out _, out _, out _, out _))
                {
                    syslist.Add(new Models.SystemNameOverride
                    {
                        Name = prevname,
                        SystemAddress = sysaddr,
                        X = next?.X,
                        Y = next?.Y,
                        Z = next?.Z,
                        ValidTo = date
                    });
                }

                if (next != null)
                {
                    syslist.Remove(next);
                    syslist.Add(next with { ValidFrom = date });
                }
                else
                {
                    syslist.Add(new Models.SystemNameOverride
                    {
                        Name = sysname,
                        SystemAddress = sysaddr,
                        X = prev?.X,
                        Y = prev?.Y,
                        Z = prev?.Z,
                        ValidFrom = date
                    });
                }
            }
        }
    }

    private void ProcessSystemOverridesCsv(Dictionary<long, List<Models.SystemNameOverride>> sysOverrides, string systemsCsv)
    {
        var fields = Settings.SystemOverridesCsv.Fields;

        foreach (var line in CsvReader.ReadFromText(systemsCsv))
        {
            if (GetCsvField(line, fields.SystemName) is string systemName
                && long.TryParse(GetCsvField(line, fields.SystemAddress), out var systemAddress))
            {
                decimal? x = null;
                decimal? y = null;
                decimal? z = null;

                if (decimal.TryParse(GetCsvField(line, fields.X), out var vx)
                    && decimal.TryParse(GetCsvField(line, fields.Y), out var vy)
                    && decimal.TryParse(GetCsvField(line, fields.Z), out var vz))
                {
                    (x, y, z) = (vx, vy, vz);
                }

                if (!sysOverrides.TryGetValue(systemAddress, out var syslist))
                {
                    sysOverrides[systemAddress] = syslist = [];
                }

                if (!syslist.Any(e => string.Equals(e.Name, systemName, StringComparison.OrdinalIgnoreCase)))
                {
                    syslist.Add(new Models.SystemNameOverride
                    {
                        Name = systemName,
                        SystemAddress = systemAddress,
                        X = x,
                        Y = y,
                        Z = z
                    });
                }
            }
        }
    }

    private void ProcessSystemOverridesJson(Dictionary<long, List<Models.SystemNameOverride>> sysOverrides, string systemsJson)
    {
        var fields = Settings.SystemOverridesJson.Fields;
        var systems = JsonConvert.DeserializeObject<List<JObject>>(
            systemsJson,
            new JsonSerializerSettings
            {
                FloatParseHandling = FloatParseHandling.Decimal,
                DateParseHandling = DateParseHandling.None
            }
        ) ?? [];

        foreach (var system in systems)
        {
            if (system.SelectToken(fields.SystemAddress)?.Value<long?>() is long systemAddress
                && system.SelectToken(fields.SystemName)?.Value<string?>() is string systemName)
            {
                decimal? x = null;
                decimal? y = null;
                decimal? z = null;

                if (system.SelectToken(fields.X)?.Value<decimal?>() is decimal vx
                    && system.SelectToken(fields.Y)?.Value<decimal?>() is decimal vy
                    && system.SelectToken(fields.Z)?.Value<decimal?>() is decimal vz)
                {
                    (x, y, z) = (vx, vy, vz);
                }

                if (!sysOverrides.TryGetValue(systemAddress, out var syslist))
                {
                    sysOverrides[systemAddress] = syslist = [];
                }

                syslist.Add(new Models.SystemNameOverride
                {
                    Name = systemName,
                    SystemAddress = systemAddress,
                    X = x,
                    Y = y,
                    Z = z
                });
            }
        }
    }

    private void DownloadGameVersions(string filename)
    {
        var settings = Settings.GameVersionDatesCsv;

        var versions = new List<Models.GameVersionDate>();

        if (!string.IsNullOrWhiteSpace(settings.URI))
        {
            var versionsCsv = HttpClient.GetStringAsync(settings.URI).Result;
            ProcessGameVersionsCsv(versions, versionsCsv);
        }

        if (!string.IsNullOrWhiteSpace(settings.Filename))
        {
            var versionsCsv = File.ReadAllText(settings.Filename);
            ProcessGameVersionsCsv(versions, versionsCsv);
        }

        if (versions.Count != 0)
        {
            using (var file = File.Open(filename + ".tmp", FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using var writer = new StreamWriter(file, new UTF8Encoding(false));

                foreach (var ver in versions)
                {
                    writer.WriteLine(JsonConvert.SerializeObject(ver, Formatting.None));
                }
            }

            File.Move(filename + ".tmp", filename, true);
        }
    }

    private void ProcessGameVersionsCsv(List<Models.GameVersionDate> versions, string versionsCsv)
    {
        var fields = Settings.GameVersionDatesCsv.Fields;

        foreach (var line in CsvReader.ReadFromText(versionsCsv))
        {
            if (DateTime.TryParse(GetCsvField(line, fields.UpdateTime), out var updateTime))
            {
                updateTime = DateTime.SpecifyKind(updateTime, DateTimeKind.Utc);

                var updateStart = DateTime.TryParse(GetCsvField(line, fields.UpdateStartTime), out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : (DateTime?)null;
                var updateEnd = DateTime.TryParse(GetCsvField(line, fields.UpdateEndTime), out dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : (DateTime?)null;
                var alphaBetaFlag = GetCsvField(line, fields.IsAlphaOrBeta);

                var isAlphaOrBeta = (bool.TryParse(alphaBetaFlag, out var alphaBetaBool) && alphaBetaBool)
                                 || (int.TryParse(alphaBetaFlag, out var alphaBetaInt) && alphaBetaInt != 0)
                                 || alphaBetaFlag?.ToLowerInvariant() == "y";

                var seasonVersions = new List<(string? Season, string? Version)>();

                foreach (var hdr in line.Headers.Where(e => e.StartsWith(fields.VersionPrefix)))
                {
                    if (hdr.Split('_') is [_, string season]
                        && GetCsvField(line, hdr) is string version)
                    {
                        seasonVersions.Add((season, version));
                    }
                }

                var seasonHdrs = new[]
                {
                        ("1.0", fields.Version_1_0),
                        ("Horizons", fields.Version_Horizons),
                        ("Odyssey", fields.Version_Odyssey)
                    };

                foreach (var (season, hdr) in seasonHdrs)
                {
                    seasonVersions.Add((season, GetCsvField(line, hdr)));
                }

                seasonVersions.Add((GetCsvField(line, fields.Season), GetCsvField(line, fields.Version)));

                foreach (var (season, version) in seasonVersions)
                {
                    if (!string.IsNullOrEmpty(version)
                        && version.Split(".") is { } versionParts
                        && versionParts.Length >= 2
                        && versionParts.Length <= 4
                        && versionParts.All(e => int.TryParse(e, out _)))
                    {
                        List<string> versionDigits = [.. versionParts, "0"];

                        while (versionDigits.Count > 2 && int.Parse(versionDigits[^1]) == 0)
                        {
                            versions.Add(new Models.GameVersionDate
                            {
                                Season = season,
                                Version = string.Join('.', versionDigits[..^1]),
                                UpdateTime = updateTime,
                                UpdateStartTime = updateStart,
                                UpdateEndTime = updateEnd,
                                IsAlphaOrBeta = isAlphaOrBeta
                            });

                            versionDigits.RemoveAt(versionDigits.Count - 1);
                        }
                    }
                }
            }
        }
    }
}
