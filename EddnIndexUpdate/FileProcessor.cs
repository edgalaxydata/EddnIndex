using Ionic.BZip2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace EddnIndexUpdate
{
    public partial class FileProcessor(
            IDbContextFactory<Models.EDDNContext> contextFactory,
            ILogger<FileProcessor> logger,
            IOptions<FileProcessorSettings> options
        )
    {
        private readonly IDbContextFactory<Models.EDDNContext> ContextFactory = contextFactory;
        private readonly ILogger Logger = logger;
        private readonly FileProcessorSettings Settings = options.Value;

        private readonly Dictionary<string, Models.SignalInfoSet> SignalInfoSetCache = [];
        private readonly Dictionary<int, Models.SignalInfoSet> SignalInfoSetCacheById = [];
        private readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineBody> BodyInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo), Models.FileLineInfo> LineInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo), Models.FileLineStation> StationInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineNavRoute> NavRouteCache = [];
        private readonly Dictionary<(int FileId, int LineNo), Models.FileLineSignal> SignalInfoCache = [];
        private readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineBodySignal> BodySignalInfoCache = [];

        private readonly Dictionary<string, Models.File> Files = [];
        private readonly Dictionary<(string Name, string Version), Models.SoftwareInfo> Software = [];
        private readonly Dictionary<(string? Version, string? Build, bool? IsOdyssey, bool? IsHorizons), Models.GameVersionInfo> GameVersions = [];
        private readonly Dictionary<(string SignalName, string? SignalType, bool? IsStation), Models.SignalInfo> Signals = [];
        private readonly Dictionary<int, Models.SignalInfo> SignalsById = [];
        private readonly Dictionary<(string Type, int? Count, string? Category, string? SubCategory, string? Region, long? EntryID), Models.BodySignalInfo> BodySignals = [];
        private readonly Dictionary<(string? StationName, long? MarketId, string? StationType, string? SystemName, long? SystemAddress, string? BodyName), List<Models.Station>> Stations = [];

        private readonly HttpClient HttpClient = new();

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

        private void Init()
        {
            if (InitComplete) return;

            Init_Overrides();

            Init_Systems();

            Init_Bodies();

            using var ctx = ContextFactory.CreateDbContext();

            if (Files.Count == 0)
            {
                Logger.LogInformation("Loading file info");

                foreach (var file in ctx.Set<Models.File>().AsNoTracking())
                {
                    Files[file.FileName] = file;
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
                    SignalsById[s.Id] = s;
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

            InitComplete = true;
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
            SignalsById[signal.Id] = signal;
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

                    SaveUpdates(newLines, newBodyLines, newStationLines, newNavRouteEntries, newSignalEntries, newBodySignalEntries);

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

                if (line.Length < 2)
                {
                    data.IsBad = true;
                }
                else
                {
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

            SaveUpdates(newLines, newBodyLines, newStationLines, newNavRouteEntries, newSignalEntries, newBodySignalEntries);

            using (var ctx = ContextFactory.CreateDbContext())
            {
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

        private static void AddOrUpdateInfo<T, TId>(Dictionary<TId, (T Info, DateTime? FirstSeen, DateTime? LastSeen)> updates, T? entry, DateTime? gatewayTimestamp)
            where TId : unmanaged
            where T : class, Models.IHasFirstLastSeen, Models.IHasId<TId>
        {
            if (gatewayTimestamp != null && entry != null)
            {
                if (!updates.TryGetValue(entry.Id, out var info))
                {
                    info = (entry, entry.FirstSeen, entry.LastSeen);
                }

                if (info.FirstSeen == null || gatewayTimestamp < info.FirstSeen)
                {
                    updates[entry.Id] = info = info with { FirstSeen = gatewayTimestamp };
                }

                if (info.LastSeen == null || gatewayTimestamp > info.LastSeen)
                {
                    updates[entry.Id] = info with { LastSeen = gatewayTimestamp };
                }
            }
        }

        private void SaveUpdates(
                Dictionary<int, Models.FileLineInfo> newLines,
                Dictionary<(int LineNo, int EntryNum), Models.FileLineBody> newBodyLines,
                Dictionary<int, Models.FileLineStation> newStationLines,
                Dictionary<(int LineNo, int EntryNum), Models.FileLineNavRoute> newNavRouteEntries,
                Dictionary<int, Models.FileLineSignal> newSignalEntries,
                Dictionary<(int LineNo, int EntryNum), Models.FileLineBodySignal> newBodySignalEntries
            )
        {
            using (var ctx = ContextFactory.CreateDbContext())
            {
                foreach (var ent in SystemCache.Values)
                {
                    if (ent.Id <= 0)
                    {
                        ctx.Add(ent);
                    }
                }

                ctx.SaveChanges();
            }

            using (var ctx = ContextFactory.CreateDbContext())
            {
                foreach (var set in BodyCache.Values)
                {
                    foreach (var ent in set)
                    {
                        if (ent.System != null)
                        {
                            Assert(ent.System.Id > 0);
                            ent.SystemId = ent.System.Id;
                            ent.System = null;
                        }

                        if (ent.ParentSet != null)
                        {
                            Assert(ent.ParentSet.Id > 0);
                            ent.ParentSetId = ent.ParentSet.Id;
                            ent.ParentSet = null;
                        }

                        if (ent.Id <= 0)
                        {
                            ctx.Add(ent);
                        }
                    }
                }

                ctx.SaveChanges();
            }

            using (var ctx = ContextFactory.CreateDbContext())
            {
                foreach (var ent in SignalInfoSetCache.Values)
                {
                    foreach (var sig in ent.SignalSetItems)
                    {
                        if (sig.Signal != null)
                        {
                            sig.SignalInfoId = sig.Signal.Id;
                            sig.Signal = null;
                        }
                    }

                    if (ent.Id == 0)
                    {
                        ctx.Add(ent);
                    }
                }

                ctx.SaveChanges();
            }

            using (var ctx = ContextFactory.CreateDbContext())
            {
                var softwareUpdates = new Dictionary<int, (Models.SoftwareInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();
                var gameVersionUpdates = new Dictionary<int, (Models.GameVersionInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();
                var systemUpdates = new Dictionary<int, (Models.System Info, DateTime? FirstSeen, DateTime? LastSeen)>();

                foreach (var _ent in newLines.Values)
                {
                    Assert(_ent.Software?.Id != 0);
                    Assert(_ent.GameVersion?.Id != 0);
                    Assert(_ent.System?.Id != 0);

                    var software = _ent.Software;
                    var gameVersion = _ent.GameVersion;
                    var system = _ent.System;
                    var gatewayTimestamp = _ent.GatewayTimestamp;

                    var ent = _ent with
                    {
                        SoftwareId = software?.Id,
                        GameVersionId = gameVersion?.Id,
                        SystemId = system?.Id,
                        GameVersion = null,
                        Software = null,
                        System = null
                    };

                    AddOrUpdateInfo(softwareUpdates, software, gatewayTimestamp);
                    AddOrUpdateInfo(gameVersionUpdates, gameVersion, gatewayTimestamp);
                    AddOrUpdateInfo(systemUpdates, system, gatewayTimestamp);

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

                foreach (var (info, firstSeen, lastSeen) in gameVersionUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                foreach (var (info, firstSeen, lastSeen) in softwareUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                foreach (var (info, firstSeen, lastSeen) in systemUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                ctx.SaveChanges();
            }

            newLines.Clear();

            using (var ctx = ContextFactory.CreateDbContext())
            {
                var bodyUpdates = new Dictionary<long, (Models.Body Info, DateTime? FirstSeen, DateTime? LastSeen)>();

                foreach (var _ent in newBodyLines.Values)
                {
                    Assert(_ent.Body != null);
                    Assert(_ent.Body.Id != 0);

                    var body = _ent.Body;
                    var gatewayTimestamp = _ent.GatewayTimestamp;

                    var ent = _ent with
                    {
                        BodyId = _ent.Body.Id,
                        Body = null
                    };

                    AddOrUpdateInfo(bodyUpdates, body, gatewayTimestamp);

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

                foreach (var (info, firstSeen, lastSeen) in bodyUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                ctx.SaveChanges();
            }

            newBodyLines.Clear();

            using (var ctx = ContextFactory.CreateDbContext())
            {
                var stationUpdates = new Dictionary<int, (Models.Station Info, DateTime? FirstSeen, DateTime? LastSeen)>();

                foreach (var _ent in newStationLines.Values)
                {
                    Assert(_ent.Station != null);
                    Assert(_ent.Station.Id != 0);

                    var station = _ent.Station;
                    var gatewayTimestamp = _ent.GatewayTimestamp;

                    var ent = _ent with
                    {
                        StationId = _ent.Station.Id,
                        Station = null
                    };

                    AddOrUpdateInfo(stationUpdates, station, gatewayTimestamp);

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

                foreach (var (info, firstSeen, lastSeen) in stationUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                ctx.SaveChanges();
            }

            newStationLines.Clear();

            using (var ctx = ContextFactory.CreateDbContext())
            {
                var systemUpdates = new Dictionary<int, (Models.System Info, DateTime? FirstSeen, DateTime? LastSeen)>();

                foreach (var _ent in newNavRouteEntries.Values)
                {
                    Assert(_ent.System != null);
                    Assert(_ent.System.Id != 0);

                    var system = _ent.System;
                    var gatewayTimestamp = _ent.GatewayTimestamp;

                    var ent = _ent with
                    {
                        SystemId = _ent.System.Id,
                        System = null
                    };

                    AddOrUpdateInfo(systemUpdates, system, gatewayTimestamp);

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

                foreach (var (info, firstSeen, lastSeen) in systemUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                ctx.SaveChanges();
            }

            newNavRouteEntries.Clear();

            using (var ctx = ContextFactory.CreateDbContext())
            {
                var signalUpdates = new Dictionary<int, (Models.SignalInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();

                foreach (var _ent in newSignalEntries.Values)
                {
                    Assert(_ent.SignalInfoSet != null);
                    Assert(_ent.SignalInfoSet.Id != 0);

                    var siginfoset = _ent.SignalInfoSet;
                    var gatewayTimestamp = _ent.GatewayTimestamp;

                    var ent = _ent with
                    {
                        SignalSetId = _ent.SignalInfoSet.Id,
                        SystemId = _ent.System?.Id,
                        System = null,
                        SignalInfoSet = null
                    };

                    foreach (var sig in siginfoset.SignalSetItems)
                    {
                        if (SignalsById.TryGetValue(sig.SignalInfoId, out var signal))
                        {
                            AddOrUpdateInfo(signalUpdates, signal, gatewayTimestamp);
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

                foreach (var (info, firstSeen, lastSeen) in signalUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                ctx.SaveChanges();
            }

            newSignalEntries.Clear();

            using (var ctx = ContextFactory.CreateDbContext())
            {
                var signalUpdates = new Dictionary<int, (Models.BodySignalInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();

                foreach (var _ent in newBodySignalEntries.Values)
                {
                    Assert(_ent.Signal != null);
                    Assert(_ent.Signal.Id != 0);

                    var signal = _ent.Signal;
                    var gatewayTimestamp = _ent.GatewayTimestamp;

                    var ent = _ent with
                    {
                        BodySignalId = _ent.Signal.Id,
                        BodyId = _ent.Body?.Id,
                        Signal = null,
                        Body = null
                    };

                    AddOrUpdateInfo(signalUpdates, signal, gatewayTimestamp);

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

                foreach (var (info, firstSeen, lastSeen) in signalUpdates.Values)
                {
                    var entry = ctx.Attach(info);
                    entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
                    entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
                }

                ctx.SaveChanges();
            }

            newBodySignalEntries.Clear();
        }
    }
}
