using Csv;
using EddnIndexUpdate.Sectors;
using Ionic.BZip2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace EddnIndexUpdate
{
    public class FileProcessor(
            IDbContextFactory<Models.EDDNContext> contextFactory,
            ILogger<FileProcessor> logger,
            IOptions<FileProcessorSettings> options
        )
    {
        private readonly IDbContextFactory<Models.EDDNContext> ContextFactory = contextFactory;
        private readonly ILogger Logger = logger;
        private readonly FileProcessorSettings Settings = options.Value;

        private readonly Dictionary<(string? SystemName, long? SystemAddress, decimal? X, decimal? Y, decimal? Z), Models.System> SystemCache = [];
        private readonly Dictionary<int, Models.System> SystemCacheById = [];
        private readonly Dictionary<(string? BodyName, int? BodyID, string? BodyType, string? ParentJson, long? SystemNameId, long? ModSystemAddress, decimal? X, decimal? Y, decimal? Z), List<Models.Body>> BodyCache = [];
        private readonly Dictionary<long, Models.Body> BodyCacheById = [];
        private readonly Dictionary<string, Models.SignalInfoSet> SignalInfoSetCache = [];
        private readonly Dictionary<int, Models.SignalInfoSet> SignalInfoSetCacheById = [];
        private readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineBody> BodyInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo), Models.FileLineInfo> LineInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo), Models.FileLineStation> StationInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineNavRoute> NavRouteCache = [];
        private readonly Dictionary<(int FileId, int LineNo), Models.FileLineSignal> SignalInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineBodySignal> BodySignalInfoCache = [];

        private readonly Dictionary<string, Models.File> Files = [];
        private readonly Dictionary<string, Models.BodyName> BodyNames = [];
        private readonly Dictionary<string, Models.BodyDesignation> BodyDesignations = [];
        private readonly Dictionary<string, Models.SystemName> SystemNames = [];
        private readonly Dictionary<int, Models.SystemName> SystemNamesById = [];
        private readonly Dictionary<string, Models.Sector> Sectors = [];
        private readonly Dictionary<int, Models.Sector> SectorsById = [];
        private readonly Dictionary<(string Name, string Version), Models.SoftwareInfo> Software = [];
        private readonly Dictionary<(string? Version, string? Build, bool? IsOdyssey, bool? IsHorizons), Models.GameVersionInfo> GameVersions = [];
        private readonly Dictionary<(string SignalName, string? SignalType, bool? IsStation), Models.SignalInfo> Signals = [];
        private readonly Dictionary<(string Type, int? Count, string? Category, string? SubCategory, string? Region, long? EntryID), Models.BodySignalInfo> BodySignals = [];
        private readonly Dictionary<(string? StationName, long? MarketId, string? StationType, string? SystemName, long? SystemAddress, string? BodyName), List<Models.Station>> Stations = [];
        private readonly Dictionary<(int BodyID, string? BodyType, string? ParentJson), Models.ParentSet> ParentSets = [];
        private readonly Dictionary<string, Models.FilePrefixSchema> SchemasByFilePrefix = [];
        private readonly Dictionary<string, List<Models.BodyNameOverride>> BodyNameOverrides = [];
        private readonly Dictionary<string, List<Models.SystemNameOverride>> SystemNameOverrides = [];
        private readonly Dictionary<string, Models.GameVersionDate> GameVersionDates = [];

        private readonly HttpClient HttpClient = new();

        private string BodyOverridesFile => Path.Combine(Settings.BaseDir, Settings.BodyOverridesFile);

        private string SystemOverridesFile => Path.Combine(Settings.BaseDir, Settings.SystemOverridesFile);

        private string GameVersionDatesFile => Path.Combine(Settings.BaseDir, Settings.GameVersionDatesFile);

        private string MessageTypesFile => Path.Combine(Settings.BaseDir, Settings.MessageTypesFile);

        private static readonly int Version = 1;

        private bool InitComplete = false;

        [DoesNotReturn]
        public void Fail(string? message, object? extraData = null)
        {
            Logger.LogError("Assert failure:\n{message}\nExtraData={ExtraData}", message, JsonConvert.SerializeObject(extraData));

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            throw new InvalidOperationException(message);
        }

        public void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null, object? extraData = null)
        {
            if (!condition)
            {
                Fail(message, extraData);
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
            var fields = settings.Fields;

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

                    foreach (var overrides in byName.Values)
                    {
                        if (overrides.Any(o => o.BodyName != o.BodyDesignation))
                        {
                            foreach (var ent in overrides)
                            {
                                writer.WriteLine(JsonConvert.SerializeObject(ent, Formatting.None));
                            }
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
                            "R" when bodyDesig.EndsWith(" Belt") == true => BodyType.StellarRing,
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
                    else if (!TrySplitProcgenName(sysname, out _, out _, out _, out _))
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

        private void Init()
        {
            if (InitComplete) return;

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

            if (Sectors.Count == 0 || SectorsById.Count == 0)
            {
                Logger.LogInformation("Loading sectors");

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

                ctx.SaveChanges();

                foreach (var sector in Sectors.Values)
                {
                    SectorsById[sector.Id] = sector;
                }
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
                            if (!string.IsNullOrWhiteSpace(ent.SinceVersion) && GameVersionDates.TryGetValue(ent.SinceVersion, out var ver))
                            {
                                ent = ent with { ValidFrom = ver.UpdateTime };
                            }
                            else
                            {
                                ent = ent with { ValidFrom = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
                            }

                            if (!string.IsNullOrWhiteSpace(ent.UntilVersion) && GameVersionDates.TryGetValue(ent.UntilVersion, out ver))
                            {
                                ent = ent with { ValidTo = ver.UpdateTime };
                            }
                            else
                            {
                                ent = ent with { ValidTo = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc) };
                            }

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

            if (Files.Count == 0)
            {
                Logger.LogInformation("Loading file info");

                foreach (var file in ctx.Set<Models.File>().AsNoTracking())
                {
                    Files[file.FileName] = file;
                }
            }

            if (BodyNames.Count == 0)
            {
                Logger.LogInformation("Loading body names");

                foreach (var bodyname in ctx.Set<Models.BodyName>().AsNoTracking())
                {
                    BodyNames[bodyname.Name] = bodyname;
                }
            }

            if (BodyDesignations.Count == 0)
            {
                Logger.LogInformation("Loading body designations");

                foreach (var desig in ctx.Set<Models.BodyDesignation>().AsNoTracking())
                {
                    BodyDesignations[desig.Designation] = desig;
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

            if (Software.Count == 0)
            {
                Logger.LogInformation("Loading software versions");

                foreach (var sw in ctx.Set<Models.SoftwareInfo>().AsNoTracking())
                {
                    Software[(sw.SoftwareName, sw.SoftwareVersion)] = sw;
                }
            }

            if (GameVersions.Count == 0)
            {
                Logger.LogInformation("Loading game versions");

                foreach (var gv in ctx.Set<Models.GameVersionInfo>().AsNoTracking())
                {
                    GameVersions[(gv.GameVersion, gv.GameBuild, gv.IsOdyssey, gv.IsHorizons)] = gv;
                }
            }

            if (Signals.Count == 0)
            {
                Logger.LogInformation("Loading signals");
                foreach (var s in ctx.Set<Models.SignalInfo>().AsNoTracking())
                {
                    Signals[(s.SignalName, s.SignalType, s.IsStation)] = s;
                }
            }

            if (BodySignals.Count == 0)
            {
                Logger.LogInformation("Loading body signals");

                foreach (var s in ctx.Set<Models.BodySignalInfo>().AsNoTracking())
                {
                    BodySignals[(s.SignalType, s.SignalCount, s.Category, s.SubCategory, s.Region, s.EntryID)] = s;
                }
            }

            if (Stations.Count == 0)
            {
                Logger.LogInformation("Loading stations");

                foreach (var s in ctx.Set<Models.Station>().AsNoTracking())
                {
                    if (!Stations.TryGetValue((s.StationName, s.MarketId, s.StationType, s.SystemName, s.SystemAddress, s.BodyName), out var stnlist))
                    {
                        Stations[(s.StationName, s.MarketId, s.StationType, s.SystemName, s.SystemAddress, s.BodyName)] = stnlist = [];
                    }

                    stnlist.Add(s);
                }
            }

            if (ParentSets.Count == 0)
            {
                Logger.LogInformation("Loading parent sets");

                foreach (var ps in ctx.Set<Models.ParentSet>().AsNoTracking())
                {
                    ParentSets[(ps.BodyID, ps.BodyType, ps.ParentJson)] = ps;
                }
            }

            InitComplete = true;
        }

        private static bool TrySplitProcgenName(ReadOnlySpan<char> systemname, [NotNullWhen(true)] out string? sectorname, out int mid, out int n2, out int masscode)
        {
            var sn = systemname;

            int i = sn.Length - 1;

            if (i < 9) goto fail;                                   // a bc-d e0

            if (sn[i] < '0' || sn[i] > '9') goto fail;              // cepheus dark region a sector xy-z a1-[0]

            n2 = 0;
            int mult = 1;
            while (i > 8 && sn[i] >= '0' && sn[i] <= '9')
            {
                n2 += (sn[i] - '0') * mult;
                i--;
                mult *= 10;
            }

            mid = 0;
            if (sn[i] == '-')                                          // cepheus dark region a sector xy-z a1[-]0
            {
                i--;

                int vend = i;
                mult = 1;
                while (i > 8 && sn[i] >= '0' && sn[i] <= '9')          // cepheus dark region a sector xy-z a[1]-0
                {
                    mid += (sn[i] - '0') * mult;
                    i--;
                    mult *= 10;
                }

                if (i == vend) goto fail;
            }

            mid *= 26 * 26 * 26;

            if (sn[i] < 'a' || sn[i] > 'h') goto fail;              // cepheus dark region a sector xy-z [a]1-0
            masscode = (sn[i] - 'a');
            i--;
            if (sn[i] != ' ') goto fail;                            // cepheus dark region a sector xy-z[ ]a1-0
            i--;
            if (sn[i] < 'A' || sn[i] > 'Z') goto fail;              // cepheus dark region a sector xy-[z] a1-0
            mid += (sn[i] - 'A') * 26 * 26;
            i--;
            if (sn[i] != '-') goto fail;                            // cepheus dark region a sector xy[-]z a1-0
            i--;
            if (sn[i] < 'A' || sn[i] > 'Z') goto fail;              // cepheus dark region a sector x[y]-z a1-0
            mid += (sn[i] - 'A') * 26;
            i--;
            if (sn[i] < 'A' || sn[i] > 'Z') goto fail;              // cepheus dark region a sector [x]y-z a1-0
            mid += (sn[i] - 'A');
            i--;
            if (sn[i] != ' ') goto fail;                            // cepheus dark region a sector[ ]xy-z a1-0
            sectorname = new string(systemname[..i]);               // [cepheus dark region a sector] xy-z a1-0
            return true;

        fail:
            sectorname = null;
            mid = 0;
            n2 = 0;
            masscode = 0;
            return false;
        }

        private Models.Sector GetOrAddSector(string name)
        {
            if (Sectors.TryGetValue(name, out var sector)) return sector;

            using var ctx = ContextFactory.CreateDbContext();

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
            ctx.SaveChanges();
            Sectors[name] = sector;
            return sector;
        }

        [return: NotNullIfNotNull(nameof(name))]
        private long? GetOrAddSystemName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            if (TrySplitProcgenName(name, out var sectorName, out var mid, out var n2, out var masscode)
                && n2 >= 0
                && n2 < 65536
                && mid >= 0
                && mid < 0x200000
                && masscode >= 0
                && masscode < 8)
            {
                var boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);
                var checkSuffix = Models.System.GetPGSuffix(boxelid);
                Assert(name.EndsWith(checkSuffix), extraData: new { name, checkSuffix });

                var sector = GetOrAddSector(sectorName);

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

            using var ctx = ContextFactory.CreateDbContext();
            systemname = new Models.SystemName { Name = name };
            ctx.Add(systemname);
            ctx.SaveChanges();

            SystemNames[name] = systemname;
            SystemNamesById[systemname.Id] = systemname;

            return -systemname.Id;
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

            if (suffix[0] >= 'A' && suffix[0] <= 'Z' - spacePos && (suffix.Length < 6 || !suffix[1..6].SequenceEqual(" Belt")))
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
                Models.System system,
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
                Debugger.Break();
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

                    if (TrySplitProcgenName(sysNameSpan, out var sectorName, out _, out _, out _)
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
            if (bodyId is not int bid) return null;

            if (parentJson != null)
            {
                parentJson = parentJson.Replace("}, {", "},{").Replace("\": ", "\":");
            }

            if (ParentSets.TryGetValue((bid, bodyType, parentJson), out var parentSet))
            {
                return parentSet.Id;
            }

            int? parentSetId = null;

            if (parentJson != null && parentJson.StartsWith('[') && parentJson.EndsWith(']'))
            {
                var parentEntry = parentJson[1..^1];
                string? parentParentJson = null;

                if (parentJson?.Contains("},") == true)
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
                BodyID = bid,
                BodyType = bodyType,
                ParentJson = parentJson,
                ParentSetId = parentSetId
            };

            ctx.Add(set);
            ctx.SaveChanges();

            ParentSets[(bid, bodyType, parentJson)] = set;

            return set.Id;
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

        private Models.System GetOrAddSystem(string? name, long? systemAddress, decimal? x, decimal? y, decimal? z)
        {
            x = RoundCoords(x);
            y = RoundCoords(y);
            z = RoundCoords(z);

            if (SystemCache.TryGetValue((name, systemAddress, x, y, z), out var system))
            {
                return system;
            }

            var nameid = GetOrAddSystemName(name);
            var modsysaddr = Models.System.SystemAddressToModSystemAddress(systemAddress);
            var revsysaddr = Models.System.ModSystemAddressToSystemAddress(modsysaddr);
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
                    namemodsysaddr ??= Models.System.SystemAddressToModSystemAddress(ovr.SystemAddress);

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

            var namesysaddr = Models.System.ModSystemAddressToSystemAddress(namemodsysaddr);
            var revnamemodsysaddr = Models.System.SystemAddressToModSystemAddress(namesysaddr);
            Assert(namemodsysaddr == revnamemodsysaddr, extraData: new { namemodsysaddr, namesysaddr, revnamemodsysaddr });

            systemAddress ??= namesysaddr;

            using var ctx = ContextFactory.CreateDbContext();

            system =
                ctx.Set<Models.System>()
                   .AsNoTracking()
                   .FirstOrDefault(e => e.SystemNameId == nameid
                                     && e.ModSystemAddress == modsysaddr
                                     && e.X == x
                                     && e.Y == y
                                     && e.Z == z);

            if (system != null)
            {
                if (!SystemCacheById.TryGetValue(system.Id, out var byid))
                {
                    SystemCacheById[system.Id] = byid = system;
                }

                system = byid;

                SystemCache.Add((name, systemAddress, x, y, z), system);

                if (systemAddress != null && modsysaddr == namemodsysaddr)
                {
                    SystemCache.Add((name, null, x, y, z), system);
                }

                return system;
            }

            system = new Models.System
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

            SystemCache.Add((name, systemAddress, x, y, z), system);

            if (systemAddress != null && modsysaddr == namemodsysaddr)
            {
                SystemCache.Add((name, null, x, y, z), system);
            }

            return system;
        }

        private static readonly int[] DegIncs = [.. new[]
        {
            -362,
            -361,
            -360,
            -359,
            -358,
            -2,
            -1,
            0,
            1,
            2,
            358,
            359,
            360,
            361,
            362
        }.OrderBy(e => Math.Abs(((e + 362) % 360) - 2))
         .ThenBy(Math.Abs)];

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

        private bool TryGetMatchingBody(
                List<Models.Body> bodiesList,
                decimal? argOfPeriapsis,
                decimal? inclination,
                decimal? semiMajorAxis,
                [NotNullWhen(true)] out Models.Body? body,
                out short? semiMajorAxisError,
                out short? inclinationError,
                out short? argOfPeriapsisError
            )
        {
            body = null;
            argOfPeriapsisError = null;
            inclinationError = null;
            semiMajorAxisError = null;

            foreach (var item in bodiesList)
            {
                if (item.SemiMajorAxis.HasValue != semiMajorAxis.HasValue) continue;
                if (item.ArgOfPeriapsis.HasValue != argOfPeriapsis.HasValue) continue;
                if (item.Inclination.HasValue != inclination.HasValue) continue;

                var smadiff = (semiMajorAxis ?? 0) * DecimalRecipPow10(item.SemiMajorAxisScale) - (item.SemiMajorAxis ?? 0);

                if (smadiff <= -0.001m || smadiff >= 0.001m) continue;
                semiMajorAxisError = (short)Math.Round(smadiff * 1000000);

                var aopdiff = (argOfPeriapsis ?? 0) - (item.ArgOfPeriapsis ?? 0);

                while (aopdiff <= -180) aopdiff += 360;
                while (aopdiff > 180) aopdiff -= 360;

                if (aopdiff <= -0.001m || aopdiff >= 0.001m) continue;
                argOfPeriapsisError = (short)Math.Round(aopdiff * 1000000);

                var incdiff = (inclination ?? 0) - (item.Inclination ?? 0);

                while (incdiff <= -180) incdiff += 360;
                while (incdiff > 180) incdiff -= 360;

                if (incdiff <= -0.001m || incdiff >= 0.001m) continue;
                inclinationError = (short)Math.Round(incdiff * 1000000);

                Assert(body == null, extraData: bodiesList);
                Assert(incdiff >= -0.001m
                    && incdiff <= 0.001m
                    && aopdiff >= -0.001m
                    && aopdiff <= 0.001m
                    && smadiff >= -0.001m
                    && smadiff <= 0.001m,
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

        private (Models.Body body, short? smaerror, short? aoperror, short? incerror) GetOrAddBody(
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
                Models.System system
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
                    ctx.Set<Models.Body>()
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

                if (overrides.Count > 1)
                {
                    Debugger.Break();
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

            body = new Models.Body
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

        private Models.SoftwareInfo GetOrAddSoftware(string softwareName, string softwareVersion)
        {
            if (Software.TryGetValue((softwareName, softwareVersion), out var software))
            {
                return software;
            }

            using var ctx = ContextFactory.CreateDbContext();
            software = new Models.SoftwareInfo
            {
                SoftwareName = softwareName,
                SoftwareVersion = softwareVersion
            };

            ctx.Add(software);
            ctx.SaveChanges();

            Software[(softwareName, softwareVersion)] = software;

            return software;
        }

        private Models.GameVersionInfo GetOrAddGameVersion(string? gamebuild, string? gameversion, bool? isOdyssey, bool? isHorizons)
        {
            if (GameVersions.TryGetValue((gameversion, gamebuild, isOdyssey, isHorizons), out var version))
            {
                return version;
            }

            using var ctx = ContextFactory.CreateDbContext();
            version = new Models.GameVersionInfo
            {
                GameBuild = gamebuild,
                GameVersion = gameversion,
                IsOdyssey = isOdyssey,
                IsHorizons = isHorizons,
            };

            ctx.Add(version);
            ctx.SaveChanges();

            GameVersions[(gameversion, gamebuild, isOdyssey, isHorizons)] = version;

            return version;
        }

        private Models.Station GetOrAddStation(string? stationName, long? marketId, string? stationType, string? systemName, long? systemAddress, string? bodyName, decimal? latitude, decimal? longitude)
        {
            if (stationType == "FleetCarrier" || (marketId >= 3700_000_000 && marketId < 3789_600_000))
            {
                stationType ??= "FleetCarrier";
                systemName = null;
                systemAddress = null;
                bodyName = null;
                latitude = null;
                longitude = null;
            }

            if (!Stations.TryGetValue((stationName, marketId, stationType, systemName, systemAddress, bodyName), out var stnlist))
            {
                Stations[(stationName, marketId, stationType, systemName, systemAddress, bodyName)] = stnlist = [];
            }

            foreach (var stn in stnlist)
            {
                if (stn.Latitude <= latitude - 0.001m || stn.Latitude >= latitude + 0.001m) continue;
                if (stn.Longitude <= longitude - 0.001m || stn.Longitude >= longitude + 0.001m) continue;

                return stn;
            }

            using var ctx = ContextFactory.CreateDbContext();

            var station = new Models.Station
            {
                StationName = stationName,
                MarketId = marketId,
                StationType = stationType,
                SystemName = systemName,
                SystemAddress = systemAddress,
                BodyName = bodyName,
                Latitude = latitude,
                Longitude = longitude
            };
            ctx.Add(station);
            ctx.SaveChanges();
            stnlist.Add(station);
            return station;
        }

        private Models.SignalInfo GetOrAddSignal(string name, string? type, bool? isStation)
        {
            if (Signals.TryGetValue((name, type, isStation), out var signal))
            {
                return signal;
            }

            using var ctx = ContextFactory.CreateDbContext();
            signal = new Models.SignalInfo
            {
                SignalName = name,
                SignalType = type,
                IsStation = isStation
            };
            ctx.Add(signal);
            ctx.SaveChanges();
            Signals[(name, type, isStation)] = signal;
            return signal;
        }

        private Models.SignalInfoSet GetOrAddSignalInfoSet(ICollection<Models.SignalInfo> signals)
        {
            var signalIds = signals.Select(e => e.Id).Order().ToList();
            var signalIdsJson =
                JsonConvert.SerializeObject(
                    signalIds
                        .GroupBy(e => e)
                        .Select(g => g.Count() == 1 ? (object)g.Key : new[] { g.Key, g.Count() })
                );

            if (SignalInfoSetCache.TryGetValue(signalIdsJson, out var signalSet))
            {
                return signalSet;
            }

            using var ctx = ContextFactory.CreateDbContext();

            var firstSigId = signalIds[0];
            var lastSigId = signalIds[^1];
            var signalCount = signalIds.Count;

            signalSet =
                ctx.Set<Models.SignalInfoSet>()
                   .Include(e => e.SignalSetItems)
                   .FirstOrDefault(e => e.FirstSignalId == firstSigId && e.LastSignalId == lastSigId && e.SignalCount == signalCount && e.SignalSetJson == signalIdsJson);

            if (signalSet != null)
            {
                if (!SignalInfoSetCacheById.TryGetValue(signalSet.Id, out var byid))
                {
                    SignalInfoSetCacheById[signalSet.Id] = byid = signalSet;
                }

                SignalInfoSetCache[signalIdsJson] = byid;
                return byid;
            }

            signalSet = new Models.SignalInfoSet
            {
                SignalSetJson = signalIdsJson,
                FirstSignalId = signalIds[0],
                LastSignalId = signalIds[^1],
                SignalCount = signalIds.Count,
                SignalSetItems = [.. signals.GroupBy(e => e.Id).Select(g => new Models.SignalInfoSetItem
                {
                    SignalInfoId = g.Key,
                    Signal = g.First(),
                    Count = g.Count()
                })]
            };

            SignalInfoSetCache[signalIdsJson] = signalSet;
            return signalSet;
        }

        private Models.BodySignalInfo GetOrAddBodySignal(string type, int? count, string? category = null, string? subcategory = null, string? region = null, long? entryId = null)
        {
            if (BodySignals.TryGetValue((type, count, category, subcategory, region, entryId), out var signal))
            {
                return signal;
            }

            using var ctx = ContextFactory.CreateDbContext();
            signal = new Models.BodySignalInfo
            {
                SignalType = type,
                SignalCount = count,
                Category = category,
                SubCategory = subcategory,
                Region = region,
                EntryID = entryId
            };
            ctx.Add(signal);
            ctx.SaveChanges();
            BodySignals[(type, count, category, subcategory, region, entryId)] = signal;
            return signal;
        }

        private bool TryProcessLineHeader(ref Utf8JsonReader reader, ref FileLineData data)
        {
            string? softwareName = null;
            string? softwareVersion = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 1) continue;
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 1) break;

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 2)
                {
                    var name = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((name, reader.TokenType))
                    {
                        case ("gatewayTimestamp", JsonTokenType.String) when (reader.TryGetDateTime(out var gwts)):
                            data.GatewayTimestamp = gwts;
                            break;
                        case ("gamebuild", JsonTokenType.String):
                            data.GameBuild = reader.GetString();
                            break;
                        case ("gameversion", JsonTokenType.String):
                            data.GameVersion = reader.GetString();
                            break;
                        case ("softwareName", JsonTokenType.String):
                            softwareName = reader.GetString();
                            break;
                        case ("softwareVersion", JsonTokenType.String):
                            softwareVersion = reader.GetString();
                            break;
                        case ("uploaderID", JsonTokenType.String):
                            break;
                        case ("manuallyApproved", JsonTokenType.False or JsonTokenType.True):
                            break;
                        default:
                            Fail($"Unknown header field {name}");
                            break;
                    }
                }
            }

            //Assert(softwareName != null && softwareVersion != null);

            data.Software = GetOrAddSoftware(softwareName ?? "", softwareVersion ?? "");

            return true;
        }

        private bool TryProcessNavRoute(ref Utf8JsonReader reader, ref FileLineData data)
        {
            long? systemAddress = null;
            string? systemName = null;
            decimal? x = null;
            decimal? y = null;
            decimal? z = null;
            int itemnum = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
                {
                    systemName = null;
                    systemAddress = null;
                    x = null;
                    y = null;
                    z = null;
                    itemnum++;
                }

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
                {
                    Assert(systemName != null);

                    data.NavRouteSystems[itemnum] = GetOrAddSystem(systemName, systemAddress, x, y, z);
                }

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 4)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((propname, reader.TokenType))
                    {
                        case ("SystemAddress", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            systemAddress = dv;
                            break;
                        case ("StarSystem", JsonTokenType.String):
                            systemName = reader.GetString();
                            break;
                        case ("StarPos", JsonTokenType.StartArray):
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var xv));
                            Assert(xv > -100000 && xv < 100000);
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var yv));
                            Assert(yv > -100000 && yv < 100000);
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var zv));
                            Assert(zv > -100000 && zv < 100000);
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.EndArray);
                            x = xv;
                            y = yv;
                            z = zv;
                            break;
                    }
                }
            }

            return true;
        }

        private bool TryProcessSignals(ref Utf8JsonReader reader, ref FileLineData data)
        {
            string? name = null;
            string? type = null;
            bool? isStation = null;
            int itemnum = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
                {
                    name = null;
                    type = null;
                    isStation = null;
                    itemnum++;
                }

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
                {
                    if (name != null)
                    {
                        data.Signals[itemnum] = GetOrAddSignal(name, type, isStation);
                    }
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((propname, reader.TokenType))
                    {
                        case ("SignalName", JsonTokenType.String):
                            name = reader.GetString();
                            break;
                        case ("SignalType", JsonTokenType.String):
                            type = reader.GetString();
                            break;
                        case ("IsStation", JsonTokenType.True or JsonTokenType.False) :
                            isStation = reader.GetBoolean();
                            break;
                    }
                }
            }

            return true;
        }

        private bool TryProcessBodySignals(ref Utf8JsonReader reader, ref FileLineData data)
        {
            string? type = null;
            int? count = null;
            int itemnum = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
                {
                    type = null;
                    count = null;
                    itemnum++;
                }

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
                {
                    if (type != null)
                    {
                        data.BodySignals[itemnum] = GetOrAddBodySignal(type, count);
                    }
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((propname, reader.TokenType))
                    {
                        case ("Count", JsonTokenType.Number) when (reader.TryGetInt32(out var cnt)):
                            count = cnt;
                            break;
                        case ("Type", JsonTokenType.String):
                            type = reader.GetString();
                            break;
                    }
                }
            }

            return true;
        }

        private bool TryProcessRings(ref Utf8JsonReader reader, ref FileLineData data)
        {
            string? ringName = null;
            decimal? innerRadius = null;
            decimal? outerRadius = null;
            int itemnum = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
                {
                    ringName = null;
                    itemnum++;
                }

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
                {
                    if (ringName != null)
                    {
                        data.RingData[itemnum] = (ringName, innerRadius, outerRadius);
                    }
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((propname, reader.TokenType))
                    {
                        case ("Name", JsonTokenType.String):
                            ringName = reader.GetString();
                            break;
                        case ("InnerRadius", JsonTokenType.Number) when reader.TryGetDecimal(out var dv):
                            innerRadius = dv;
                            break;
                        case ("OuterRadius", JsonTokenType.Number) when reader.TryGetDecimal(out var dv):
                            outerRadius = dv;
                            break;
                    }
                }
            }

            return true;
        }

        private bool TryProcessLineMessage(ref Utf8JsonReader reader, ReadOnlySpan<byte> json, ref FileLineData data)
        {
            string? bodyName = null;
            string? bodyType = null;
            int? bodyId = null;
            long? systemAddress = null;
            long? marketId = null;
            string? systemName = null;
            string? stationName = null;
            string? stationType = null;
            string? parentsJson = null;
            string? codexName = null;
            string? codexCategory = null;
            string? codexSubCategory = null;
            string? codexRegion = null;
            long? codexEntryId = null;
            decimal? x = null;
            decimal? y = null;
            decimal? z = null;
            decimal? argOfPeriapsis = null;
            decimal? inclination = null;
            decimal? semiMajorAxis = null;
            decimal? latitude = null;
            decimal? longitude = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 1) break;

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 2)
                {
                    var propname = reader.GetString();

                    Assert(propname != null);

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    data.MessageKeyCounts[(propname, reader.TokenType)] = data.MessageKeyCounts.GetValueOrDefault((propname, reader.TokenType)) + 1;

                    switch ((propname, reader.TokenType))
                    {
                        case ("Body" or "BodyName", JsonTokenType.String):
                            bodyName = reader.GetString();
                            break;
                        case ("BodyID", JsonTokenType.Number) when (reader.TryGetInt32(out int bid)):
                            bodyId = bid;
                            break;
                        case ("BodyType", JsonTokenType.String):
                            bodyType = reader.GetString();
                            break;
                        case ("Parents", JsonTokenType.StartArray):
                            var pos = (int)reader.TokenStartIndex;
                            reader.Skip();
                            var span = json[pos..(int)(reader.TokenStartIndex + 1)];
                            parentsJson = Encoding.UTF8.GetString(span);
                            break;
                        case ("Periapsis", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            argOfPeriapsis = dv;
                            break;
                        case ("OrbitalInclination", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            inclination = dv;
                            break;
                        case ("SemiMajorAxis", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            semiMajorAxis = dv;
                            break;
                        case ("SystemAddress", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            systemAddress = dv;
                            break;
                        case ("StarSystem" or "System" or "SystemName" or "systemName", JsonTokenType.String):
                            systemName = reader.GetString();
                            break;
                        case ("MarketID" or "marketId", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            marketId = dv;
                            break;
                        case ("StationName" or "stationName", JsonTokenType.String):
                            stationName = reader.GetString();
                            break;
                        case ("CarrierID", JsonTokenType.String):
                            stationName = reader.GetString();
                            stationType ??= "FleetCarrier";
                            break;
                        case ("Name", JsonTokenType.String) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/approachsettlement/1") == true):
                            stationName = reader.GetString();
                            break;
                        case ("StationType", JsonTokenType.String):
                            stationType = reader.GetString();
                            break;
                        case ("Latitude", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            latitude = Math.Round(dv, 6);
                            break;
                        case ("Longitude", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            longitude = Math.Round(dv, 6);
                            break;
                        case ("Name", JsonTokenType.String) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/codexentry/1") == true):
                            codexName = reader.GetString();
                            break;
                        case ("Category", JsonTokenType.String):
                            codexCategory = reader.GetString();
                            break;
                        case ("SubCategory", JsonTokenType.String):
                            codexSubCategory = reader.GetString();
                            break;
                        case ("Region", JsonTokenType.String):
                            codexRegion = reader.GetString();
                            break;
                        case ("EntryID", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            codexEntryId = dv;
                            break;
                        case ("StarPos", JsonTokenType.StartArray):
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var xv));
                            Assert(xv > -100000 && xv < 100000);
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var yv));
                            Assert(yv > -100000 && yv < 100000);
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var zv));
                            Assert(zv > -100000 && zv < 100000);
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.EndArray);
                            x = xv;
                            y = yv;
                            z = zv;
                            break;
                        case ("signals", JsonTokenType.StartArray) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fsssignaldiscovered/1") == true):
                            Assert(TryProcessSignals(ref reader, ref data));
                            break;
                        case ("Signals", JsonTokenType.StartArray) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fsssignaldiscovered/1") == false):
                            Assert(TryProcessBodySignals(ref reader, ref data));
                            break;
                        case ("Route", JsonTokenType.StartArray):
                            Assert(TryProcessNavRoute(ref reader, ref data));
                            break;
                        case ("Rings", JsonTokenType.StartArray):
                            Assert(TryProcessRings(ref reader, ref data));;
                            break;
                        case ("odyssey", JsonTokenType.True or JsonTokenType.False):
                            data.IsOdyssey = reader.GetBoolean();
                            break;
                        case ("horizons", JsonTokenType.True or JsonTokenType.False):
                            data.IsHorizons = reader.GetBoolean();
                            break;
                        case ("event", JsonTokenType.String):
                            break;
                        case ("timestamp", JsonTokenType.String) when (reader.TryGetDateTime(out var ts)):
                            data.Timestamp = ts;
                            break;
                    }
                }
            }

            if (systemName != null)
            {
                var system = GetOrAddSystem(systemName, systemAddress, x, y, z);
                data.System = system;

                if (bodyName != null)
                {
                    var (body, smaerror, incerror, aoperror) = GetOrAddBody(bodyName, systemName, bodyId, bodyType, parentsJson, argOfPeriapsis, inclination, semiMajorAxis, data.Timestamp, data.GameVersion, system);
                    data.Body = body;
                    data.SemiMajorAxisError = smaerror;
                    data.InclinationError = incerror;
                    data.ArgOfPeriapsisError = aoperror;
                }

                foreach (var (itemnum, (name, innerRad, outerRad)) in data.RingData)
                {
                    data.SubBodies[itemnum] = GetOrAddBody(name, systemName, null, null, null, 0, 0, (innerRad + outerRad) / 2, data.Timestamp, data.GameVersion, system);
                }
            }
            else if (bodyName != null)
            {
                Fail("Body Name without System Name");
            }
            else if (stationName != null)
            {
                if (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fcmaterials_capi/1") != true
                    && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/dockingdenied/1") != true
                    && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/dockinggranted/1") != true
                    && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fcmaterials_journal/1") != true)
                {
                    Fail($"Unknown schema {data.Schema}");
                }
            }

            if (codexName != null && data.BodySignals.Count == 0)
            {
                data.BodySignals[0] = GetOrAddBodySignal(codexName, 0, codexCategory, codexSubCategory, codexRegion, codexEntryId);
                data.Latitude = latitude;
                data.Longitude = longitude;
            }

            if (stationName != null || marketId != null)
            {
                data.Station = GetOrAddStation(stationName, marketId, stationType, systemName, systemAddress, bodyName, latitude, longitude);
                data.Latitude = latitude;
                data.Longitude = longitude;
            }

            return true;
        }

        private bool TryProcessLine(ReadOnlySpan<byte> line, ref FileLineData data)
        {
            var reader = new Utf8JsonReader(line);
            bool gotSchema = false;
            bool gotHeader = false;
            bool gotMessage = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 0) continue;

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1)
                {
                    var name = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((name, reader.TokenType))
                    {
                        case ("$schemaRef", JsonTokenType.String):
                            data.Schema = reader.GetString();
                            gotSchema = true;
                            break;
                        case ("header", JsonTokenType.StartObject):
                            Assert(TryProcessLineHeader(ref reader, ref data));
                            gotHeader = true;
                            break;
                        case ("message", JsonTokenType.StartObject):
                            Assert(TryProcessLineMessage(ref reader, line, ref data));
                            gotMessage = true;
                            break;
                        default:
                            break;
                    }
                }
            }

            data.GameVersionInfo = GetOrAddGameVersion(data.GameBuild, data.GameVersion, data.IsOdyssey, data.IsHorizons);

            return gotSchema && gotMessage && gotHeader;
        }

        private void FillCacheForFile(int fileid)
        {
            using var ctx = ContextFactory.CreateDbContext();

            foreach (var line in ctx.Set<Models.FileLineInfo>().Where(e => e.FileId == fileid).AsNoTracking())
            {
                LineInfoCache[(line.FileId, line.LineNo)] = line;
            }

            foreach (var line in ctx.Set<Models.FileLineBody>().Where(e => e.FileId == fileid).AsNoTracking())
            {
                BodyInfoCache[(line.FileId, line.LineNo, line.EntryNum)] = line;
            }

            foreach (var line in ctx.Set<Models.FileLineStation>().Where(e => e.FileId == fileid).AsNoTracking())
            {
                StationInfoCache[(line.FileId, line.LineNo)] = line;
            }

            foreach (var line in ctx.Set<Models.FileLineNavRoute>().Where(e => e.FileId == fileid).AsNoTracking())
            {
                NavRouteCache[(line.FileId, line.LineNo, line.EntryNum)] = line;
            }

            foreach (var line in ctx.Set<Models.FileLineSignal>().Where(e => e.FileId == fileid).AsNoTracking())
            {
                SignalInfoCache[(line.FileId, line.LineNo)] = line;
            }

            foreach (var line in ctx.Set<Models.FileLineBodySignal>().Where(e => e.FileId == fileid).AsNoTracking())
            {
                BodySignalInfoCache[(line.FileId, line.LineNo, line.EntryNum)] = line;
            }
        }

        public void ProcessFile(string filepath)
        {
            Init();

            if (!File.Exists(filepath))
            {
                return;
            }

            var fileinfo = new FileInfo(filepath);

            var filename = Path.GetFileName(filepath);
            bool test = false;

            if (Path.GetDirectoryName(filepath) is string filedir && Path.GetFileName(filedir) is string lastdir)
            {
                if (lastdir == "beta" || lastdir == "beta-data")
                {
                    filename = "beta/" + filename;
                    test = true;
                }
                else if (lastdir == "dev" || lastdir == "dev-data")
                {
                    filename = "dev/" + filename;
                    test = true;
                }
            }

            if (!Files.TryGetValue(filename, out var file))
            {
                if (filename.Split('-') is not [.. { } parts, string yearstr, string monthstr, string dayext]
                    || dayext.Split('.', 2) is not [string daystr, string ext]
                    || daystr.Length != 2
                    || monthstr.Length != 2
                    || yearstr.Length != 4
                    || !int.TryParse(daystr, out var day)
                    || day < 1
                    || day > 31
                    || !int.TryParse(monthstr, out var month)
                    || month < 1
                    || month > 12
                    || !int.TryParse(yearstr, out var year)
                    || year < 2014)
                {
                    throw new ArgumentException("Bad filename", nameof(filepath));
                }

                var date = new DateOnly(year, month, day);
                var prefix = string.Join('-', parts);
                prefix = prefix.Split('/')[^1];
                string? eventType = null;

                if (!SchemasByFilePrefix.TryGetValue(prefix, out var primarySchema)
                    && prefix.Split(".") is [string s, string t]
                    && SchemasByFilePrefix.TryGetValue(s, out primarySchema))
                {
                    eventType = t;
                }

                test |= prefix.StartsWith("Test-");

                using var ctx = ContextFactory.CreateDbContext();

                file = new Models.File
                {
                    FileName = filename,
                    Date = date,
                    PrimarySchema = primarySchema?.PrimarySchema,
                    EventType = primarySchema?.EventType ?? eventType,
                    IsTest = primarySchema?.IsTest == true || test
                };

                ctx.Add(file);
                ctx.SaveChanges();

                Files[filename] = file;
            }

            if (file.CompressedSize == fileinfo.Length
                && file.UncompressedSize != null
                && file.LineCount != null
                && file.ErrorCount == 0
                && file.ProcessedVersion == Version)
            {
                return;
            }

            Logger.LogInformation("Processing {Filename}", filename);
            Logger.LogInformation(
                "Current: S:{CurLength} U:{CurUncLen} L:{CurLineCount} E:{CurErrorCount} V:{CurVersion} -> S:{UpdLength} V:{UpdVersion}",
                file.CompressedSize,
                file.UncompressedSize,
                file.LineCount,
                file.ErrorCount,
                file.ProcessedVersion,
                fileinfo.Length,
                Version
            );

            FillCacheForFile(file.Id);

            var newLines = new Dictionary<int, Models.FileLineInfo>();
            var newBodyLines = new Dictionary<(int LineNo, int EntryNum), Models.FileLineBody>();
            var newStationLines = new Dictionary<int, Models.FileLineStation>();
            var newNavRouteEntries = new Dictionary<(int LineNo, int EntryNum), Models.FileLineNavRoute>();
            var newSignalEntries = new Dictionary<int, Models.FileLineSignal>();
            var newBodySignalEntries = new Dictionary<(int LineNo, int EntryNum), Models.FileLineBodySignal>();

            int lineCount = 0;
            int systemLineCount = 0;
            int stationLineCount = 0;
            int navRouteSystemCount = 0;
            int bodyLineCount = 0;
            int signalCount = 0;
            int bodySignalCount = 0;
            int errorCount = 0;

            Stream stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            if (filename.EndsWith(".bz2"))
            {
                stream = new BZip2InputStream(stream);
            }

            using var reader = new EventReader(stream);

            var data = new FileLineData();

            while (reader.TryReadLine(out var line))
            {
                lineCount++;

                if ((lineCount % 1000) == 0)
                {
                    Console.Error.Write(".");
                    Console.Error.Flush();

                    if ((lineCount % 64000) == 0)
                    {
                        Console.Error.WriteLine($" {lineCount}");
                    }
                }

                if (LineInfoCache.TryGetValue((file.Id, lineCount), out var lineInfo) && lineInfo.ProcessedVersion == Version)
                {
                    continue;
                }

                data.Clear(file, lineCount, line.Length);

                try
                {
                    if (!TryProcessLine(line, ref data))
                    {
                        Logger.LogError("Error in file {FileName} line number {LineNo}: incomplete message", filepath, lineCount);
                        Environment.Exit(1);

                        data.IsBad = true;
                        errorCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error in file {FileName} line number {LineNo}: {Message}", filepath, lineCount, ex.Message);
                    Environment.Exit(1);

                    data.IsBad = true;
                    errorCount++;
                }


                if (data.System == null
                    && data.Body == null
                    && data.Station == null
                    && data.Signals.Count == 0
                    && data.BodySignals.Count == 0
                    && data.NavRouteSystems.Count == 0)
                {
                    Logger.LogError("Error in file {FileName} line number {LineNo}: no data available", filepath, lineCount);
                    Environment.Exit(1);
                }

                newLines[data.LineNo] = new Models.FileLineInfo
                {
                    FileId = file.Id,
                    LineNo = data.LineNo,
                    LineLength = data.LineLength,
                    GatewayTimestamp = data.GatewayTimestamp,
                    Timestamp = data.Timestamp,
                    ProcessedVersion = Version,
                    GameVersion = data.GameVersionInfo,
                    Software = data.Software,
                    System = data.System,
                    IsBad = data.IsBad,
                };

                if (data.Body != null)
                {
                    newBodyLines[(data.LineNo, 0)] = new Models.FileLineBody
                    {
                        FileId = file.Id,
                        LineNo = data.LineNo,
                        EntryNum = 0,
                        GatewayTimestamp = data.GatewayTimestamp,
                        Body = data.Body,
                        SemiMajorAxisError = data.SemiMajorAxisError == 0 ? null : data.SemiMajorAxisError,
                        InclinationError = data.InclinationError == 0 ? null : data.InclinationError,
                        ArgOfPeriapsisError = data.ArgOfPeriapsisError == 0 ? null : data.ArgOfPeriapsisError
                    };
                }

                foreach (var (entrynum, (body, smaerror, incerror, aoperror)) in data.SubBodies)
                {
                    newBodyLines[(data.LineNo, entrynum)] = new Models.FileLineBody
                    {
                        FileId = file.Id,
                        LineNo = data.LineNo,
                        EntryNum = entrynum,
                        GatewayTimestamp = data.GatewayTimestamp,
                        Body = body,
                        SemiMajorAxisError = smaerror == 0 ? null : smaerror,
                        InclinationError = incerror == 0 ? null : incerror,
                        ArgOfPeriapsisError = aoperror == 0 ? null : aoperror
                    };
                }

                if (data.Station != null)
                {
                    newStationLines[data.LineNo] = new Models.FileLineStation
                    {
                        FileId = file.Id,
                        LineNo = data.LineNo,
                        GatewayTimestamp = data.GatewayTimestamp,
                        Station = data.Station,
                        LatitudeError = data.Latitude == data.Station.Latitude ? null : (short)Math.Round((data.Latitude - data.Station.Latitude) * 1000000 ?? 0),
                        LongitudeError = data.Longitude == data.Station.Longitude ? null : (short)Math.Round((data.Longitude - data.Station.Longitude) * 1000000 ?? 0)
                    };
                }

                if (data.Signals.Count != 0)
                {
                    var signalSet = GetOrAddSignalInfoSet(data.Signals.Values);

                    newSignalEntries[data.LineNo] = new Models.FileLineSignal
                    {
                        FileId = file.Id,
                        LineNo = data.LineNo,
                        GatewayTimestamp = data.GatewayTimestamp,
                        System = data.System,
                        SignalInfoSet = signalSet
                    };
                }

                foreach (var (entnum, system) in data.NavRouteSystems)
                {
                    newNavRouteEntries[(data.LineNo, entnum)] = new Models.FileLineNavRoute
                    {
                        FileId = file.Id,
                        LineNo = data.LineNo,
                        EntryNum = entnum,
                        GatewayTimestamp = data.GatewayTimestamp,
                        System = system
                    };
                }

                foreach (var (entnum, signal) in data.BodySignals)
                {
                    newBodySignalEntries[(data.LineNo, entnum)] = new Models.FileLineBodySignal
                    {
                        FileId = file.Id,
                        LineNo = data.LineNo,
                        EntryNum = entnum,
                        GatewayTimestamp = data.GatewayTimestamp,
                        Latitude = data.Latitude,
                        Longitude = data.Longitude,
                        Signal = signal,
                        Body = data.Body
                    };
                }

                systemLineCount += data.System != null ? 1 : 0;
                bodyLineCount += data.Body != null ? 1 : 0;
                stationLineCount += data.Station != null ? 1 : 0;
                navRouteSystemCount += data.NavRouteSystems.Count;
                signalCount += data.Signals.Count;
                bodySignalCount += data.BodySignals.Count;
            }

            Console.Error.WriteLine($" {lineCount}");

            using (var ctx = ContextFactory.CreateDbContext())
            {
                foreach (var ent in SystemCache.Values)
                {
                    if (ent.Id == 0)
                    {
                        ctx.Add(ent);
                    }
                    else
                    {
                        ctx.Attach(ent);
                    }
                }

                foreach (var set in BodyCache.Values)
                {
                    foreach (var ent in set)
                    {
                        if (ent.Id == 0)
                        {
                            ctx.Add(ent);
                        }
                        else
                        {
                            ctx.Attach(ent);
                        }
                    }
                }

                foreach (var ent in SignalInfoSetCache.Values)
                {
                    foreach (var sig in ent.SignalSetItems)
                    {
                        if (sig.Signal != null && ctx.Entry(sig.Signal).State == EntityState.Detached)
                        {
                            ctx.Attach(sig.Signal);
                        }
                    }

                    if (ent.Id == 0)
                    {
                        ctx.Add(ent);
                    }
                    else
                    {
                        ctx.Attach(ent);
                    }
                }

                ctx.SaveChanges();

                foreach (var _ent in newLines.Values)
                {
                    Assert(_ent.Software?.Id != 0);
                    Assert(_ent.GameVersion?.Id != 0);
                    Assert(_ent.System?.Id != 0);

                    var ent = _ent with
                    {
                        SoftwareId = _ent.Software?.Id,
                        GameVersionId = _ent.GameVersion?.Id,
                        SystemId = _ent.System?.Id
                    };

                    if (ent.Software != null)
                    {
                        if (ctx.Entry(ent.Software).State == EntityState.Detached)
                        {
                            ctx.Attach(ent.Software);
                        }

                        if (ent.Software.FirstSeen == null || ent.GatewayTimestamp < ent.Software.FirstSeen)
                        {
                            ent.Software.FirstSeen = ent.GatewayTimestamp;
                        }

                        if (ent.Software.LastSeen == null || ent.GatewayTimestamp > ent.Software.LastSeen)
                        {
                            ent.Software.LastSeen = ent.GatewayTimestamp;
                        }
                    }

                    if (ent.GameVersion != null)
                    {
                        if (ctx.Entry(ent.GameVersion).State == EntityState.Detached)
                        {
                            ctx.Attach(ent.GameVersion);
                        }

                        if (ent.GameVersion.FirstSeen == null || ent.GatewayTimestamp < ent.GameVersion.FirstSeen)
                        {
                            ent.GameVersion.FirstSeen = ent.GatewayTimestamp;
                        }

                        if (ent.GameVersion.LastSeen == null || ent.GatewayTimestamp > ent.GameVersion.LastSeen)
                        {
                            ent.GameVersion.LastSeen = ent.GatewayTimestamp;
                        }
                    }

                    if (ent.System != null)
                    {
                        Assert(ctx.Entry(ent.System).State != EntityState.Detached);

                        if (ent.System.FirstSeen == null || ent.GatewayTimestamp < ent.System.FirstSeen)
                        {
                            ent.System.FirstSeen = ent.GatewayTimestamp;
                        }

                        if (ent.System.LastSeen == null || ent.GatewayTimestamp > ent.System.LastSeen)
                        {
                            ent.System.LastSeen = ent.GatewayTimestamp;
                        }
                    }

                    if (LineInfoCache.TryGetValue((ent.FileId, ent.LineNo), out var lineInfo))
                    {
                        var entry = ctx.Attach(lineInfo);
                        entry.Property(e => e.SystemId).CurrentValue = ent.SystemId;
                        entry.Property(e => e.GameVersionId).CurrentValue = ent.GameVersionId;
                        entry.Property(e => e.SoftwareId).CurrentValue = ent.SoftwareId;
                        entry.Property(e => e.ProcessedVersion).CurrentValue = ent.ProcessedVersion;
                        entry.Property(e => e.GatewayTimestamp).CurrentValue = ent.GatewayTimestamp;
                        entry.Property(e => e.Timestamp).CurrentValue = ent.Timestamp;
                    }
                    else
                    {
                        ctx.Add(ent);
                        LineInfoCache[(ent.FileId, ent.LineNo)] = ent;
                    }
                }

                foreach (var _ent in newBodyLines.Values)
                {
                    Assert(_ent.Body != null);
                    Assert(_ent.Body.Id != 0);

                    var ent = _ent with { BodyId = _ent.Body.Id };

                    Assert(ctx.Entry(ent.Body).State != EntityState.Detached);

                    if (ent.Body.FirstSeen == null || ent.Body.FirstSeen > ent.GatewayTimestamp)
                    {
                        ent.Body.FirstSeen = ent.GatewayTimestamp;
                    }

                    if (ent.Body.LastSeen == null || ent.Body.LastSeen < ent.GatewayTimestamp)
                    {
                        ent.Body.LastSeen = ent.GatewayTimestamp;
                    }

                    if (BodyInfoCache.TryGetValue((ent.FileId, ent.LineNo, ent.EntryNum), out var lineInfo))
                    {
                        var entry = ctx.Attach(lineInfo);
                        entry.Property(e => e.BodyId).CurrentValue = ent.BodyId;
                        entry.Property(e => e.GatewayTimestamp).CurrentValue = ent.GatewayTimestamp;
                    }
                    else
                    {
                        ctx.Add(ent);
                        BodyInfoCache[(ent.FileId, ent.LineNo, ent.EntryNum)] = ent;
                    }
                }

                foreach (var _ent in newStationLines.Values)
                {
                    Assert(_ent.Station != null);
                    Assert(_ent.Station.Id != 0);

                    var ent = _ent with { StationId = _ent.Station.Id };

                    if (ctx.Entry(ent.Station).State == EntityState.Detached)
                    {
                        ctx.Attach(ent.Station);
                    }

                    if (ent.Station.FirstSeen == null || ent.Station.FirstSeen > ent.GatewayTimestamp)
                    {
                        ent.Station.FirstSeen = ent.GatewayTimestamp;
                    }

                    if (ent.Station.LastSeen == null || ent.Station.LastSeen < ent.GatewayTimestamp)
                    {
                        ent.Station.LastSeen = ent.GatewayTimestamp;
                    }

                    if (StationInfoCache.TryGetValue((ent.FileId, ent.LineNo), out var lineInfo))
                    {
                        var entry = ctx.Attach(lineInfo);
                        entry.Property(e => e.StationId).CurrentValue = ent.StationId;
                        entry.Property(e => e.GatewayTimestamp).CurrentValue = ent.GatewayTimestamp;
                    }
                    else
                    {
                        ctx.Add(ent);
                        StationInfoCache[(ent.FileId, ent.LineNo)] = ent;
                    }
                }

                foreach (var _ent in newNavRouteEntries.Values)
                {
                    Assert(_ent.System != null);
                    Assert(_ent.System.Id != 0);

                    var ent = _ent with { SystemId = _ent.System.Id };

                    Assert(ctx.Entry(ent.System).State != EntityState.Detached);

                    if (ent.System.FirstSeen == null || ent.GatewayTimestamp < ent.System.FirstSeen)
                    {
                        ent.System.FirstSeen = ent.GatewayTimestamp;
                    }

                    if (ent.System.LastSeen == null || ent.GatewayTimestamp > ent.System.LastSeen)
                    {
                        ent.System.LastSeen = ent.GatewayTimestamp;
                    }

                    if (NavRouteCache.TryGetValue((ent.FileId, ent.LineNo, ent.EntryNum), out var lineInfo))
                    {
                        var entry = ctx.Attach(lineInfo);
                        entry.Property(e => e.SystemId).CurrentValue = ent.SystemId;
                        entry.Property(e => e.GatewayTimestamp).CurrentValue = ent.GatewayTimestamp;
                    }
                    else
                    {
                        ctx.Add(ent);
                        NavRouteCache[(ent.FileId, ent.LineNo, ent.EntryNum)] = ent;
                    }
                }

                foreach (var _ent in newSignalEntries.Values)
                {
                    Assert(_ent.SignalInfoSet != null);
                    Assert(_ent.SignalInfoSet.Id != 0);

                    var ent = _ent with { SignalSetId = _ent.SignalInfoSet.Id, SystemId = _ent.System?.Id };

                    if (ent.System != null)
                    {
                        Assert(ctx.Entry(ent.System).State != EntityState.Detached);
                    }

                    foreach (var sig in ent.SignalInfoSet.SignalSetItems)
                    {
                        if (sig.Signal != null)
                        {
                            if (sig.Signal.FirstSeen == null || ent.GatewayTimestamp < sig.Signal.FirstSeen)
                            {
                                sig.Signal.FirstSeen = ent.GatewayTimestamp;
                            }

                            if (sig.Signal.LastSeen == null || ent.GatewayTimestamp > sig.Signal.LastSeen)
                            {
                                sig.Signal.LastSeen = ent.GatewayTimestamp;
                            }
                        }
                    }

                    if (SignalInfoCache.TryGetValue((ent.FileId, ent.LineNo), out var lineInfo))
                    {
                        var entry = ctx.Attach(lineInfo);
                        entry.Property(e => e.SignalSetId).CurrentValue = ent.SignalSetId;
                        entry.Property(e => e.SystemId).CurrentValue = ent.SystemId;
                        entry.Property(e => e.GatewayTimestamp).CurrentValue = ent.GatewayTimestamp;
                    }
                    else
                    {
                        ctx.Add(ent);
                        SignalInfoCache[(ent.FileId, ent.LineNo)] = ent;
                    }
                }

                foreach (var _ent in newBodySignalEntries.Values)
                {
                    Assert(_ent.Signal != null);
                    Assert(_ent.Signal.Id != 0);

                    var ent = _ent with { BodySignalId = _ent.Signal.Id, BodyId = _ent.Body?.Id };

                    if (ent.Body != null)
                    {
                        Assert(ctx.Entry(ent.Body).State != EntityState.Detached);
                    }

                    if (ctx.Entry(ent.Signal).State == EntityState.Detached)
                    {
                        ctx.Attach(ent.Signal);
                    }

                    if (ent.Signal.FirstSeen == null || ent.GatewayTimestamp < ent.Signal.FirstSeen)
                    {
                        ent.Signal.FirstSeen = ent.GatewayTimestamp;
                    }

                    if (ent.Signal.LastSeen == null || ent.GatewayTimestamp > ent.Signal.LastSeen)
                    {
                        ent.Signal.LastSeen = ent.GatewayTimestamp;
                    }

                    if (BodySignalInfoCache.TryGetValue((ent.FileId, ent.LineNo, ent.EntryNum), out var lineInfo))
                    {
                        var entry = ctx.Attach(lineInfo);
                        entry.Property(e => e.BodySignalId).CurrentValue = ent.BodySignalId;
                        entry.Property(e => e.BodyId).CurrentValue = ent.BodyId;
                        entry.Property(e => e.GatewayTimestamp).CurrentValue = ent.GatewayTimestamp;
                        entry.Property(e => e.Latitude).CurrentValue = ent.Latitude;
                        entry.Property(e => e.Longitude).CurrentValue = ent.Longitude;
                    }
                    else
                    {
                        ctx.Add(ent);
                        BodySignalInfoCache[(ent.FileId, ent.LineNo, ent.EntryNum)] = ent;
                    }
                }

                var fileEntry = ctx.Attach(file);
                fileEntry.Property(e => e.LineCount).CurrentValue = lineCount;
                fileEntry.Property(e => e.CompressedSize).CurrentValue = fileinfo.Length;
                fileEntry.Property(e => e.UncompressedSize).CurrentValue = reader.Position;
                fileEntry.Property(e => e.SystemLineCount).CurrentValue = systemLineCount;
                fileEntry.Property(e => e.StationLineCount).CurrentValue = stationLineCount;
                fileEntry.Property(e => e.BodyLineCount).CurrentValue = bodyLineCount;
                fileEntry.Property(e => e.NavRouteSystemCount).CurrentValue = navRouteSystemCount;
                fileEntry.Property(e => e.SignalCount).CurrentValue = signalCount;
                fileEntry.Property(e => e.BodySignalCount).CurrentValue = bodySignalCount;
                fileEntry.Property(e => e.ErrorCount).CurrentValue = errorCount;
                fileEntry.Property(e => e.ProcessedVersion).CurrentValue = Version;

                ctx.SaveChanges();
            }

            SystemCache.Clear();
            SystemCacheById.Clear();

            BodyCache.Clear();
            BodyCacheById.Clear();

            SignalInfoSetCache.Clear();
            SignalInfoSetCacheById.Clear();

            LineInfoCache.Clear();
            BodyInfoCache.Clear();
            StationInfoCache.Clear();
            NavRouteCache.Clear();
            SignalInfoCache.Clear();
            BodySignalInfoCache.Clear();
        }
    }
}
