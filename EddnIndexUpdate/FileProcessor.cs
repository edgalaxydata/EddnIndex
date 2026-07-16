using EddnIndex.Common;
using EddnIndexUpdate.Options;
using Ionic.BZip2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Models = EddnIndex.Common.Models;

namespace EddnIndexUpdate;

public partial class FileProcessor(
        IDbContextFactory<Models.EDDNContext> contextFactory,
        ILogger<FileProcessor> logger,
        IOptions<FileProcessorSettings> options,
        IHttpClientFactory httpClientFactory,
        IFileSystem fileSystem
    )
{
    private readonly IDbContextFactory<Models.EDDNContext> ContextFactory = contextFactory;
    private readonly ILogger Logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;
    protected readonly FileProcessorSettings Settings = options.Value;

    protected readonly Dictionary<(string SignalSetJson, long? SystemNameId, long? SystemAddress, decimal? X, decimal? Y, decimal? Z), Models.SignalInfoSet> SignalInfoSetCache = [];
    protected readonly Dictionary<int, Models.SignalInfoSet> SignalInfoSetCacheById = [];
    protected readonly Dictionary<int, int> SignalInfoSetCounts = [];
    protected readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineBody> BodyInfoCache = [];
    protected readonly Dictionary<(int FileId, int LineNo), int> BodyInfoCounts = [];
    protected readonly Dictionary<(int FileId, int LineNo), Models.FileLineInfo> LineInfoCache = [];
    protected readonly Dictionary<(int FileId, int LineNo), Models.FileLineStation> StationInfoCache = [];
    protected readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineNavRoute> NavRouteCache = [];
    protected readonly Dictionary<(int FileId, int LineNo), int> NavRouteCounts = [];
    protected readonly Dictionary<(int FileId, int LineNo), Models.FileLineSignal> SignalInfoCache = [];
    protected readonly Dictionary<(int FileId, int LineNo), int> SignalInfoCounts = [];
    protected readonly Dictionary<(int FileId, int LineNo, int EntryNum), Models.FileLineBodySignal> BodySignalInfoCache = [];
    protected readonly Dictionary<(int FileId, int LineNo), int> BodySignalInfoCounts = [];

    protected readonly Dictionary<string, Models.FileInfo> Files = [];
    protected readonly Dictionary<(string Name, string Version), Models.SoftwareInfo> Software = [];
    protected readonly Dictionary<(string? Version, string? Build, bool? IsOdyssey, bool? IsHorizons), Models.GameVersionInfo> GameVersions = [];
    protected readonly Dictionary<(string SignalName, string? SignalType, bool? IsStation), Models.SignalInfo> Signals = [];
    protected readonly Dictionary<(string Schema, string? EventType), Models.SchemaEventInfo> SchemaEvents = [];
    protected readonly Dictionary<int, Models.SignalInfo> SignalsById = [];
    protected readonly Dictionary<(string Type, int? Count, string? Category, string? SubCategory, string? Region, long? EntryID), Models.BodySignalInfo> BodySignals = [];
    protected readonly Dictionary<(string? StationName, long? MarketId, string? StationType, string? SystemName, long? SystemAddress, string? BodyName), List<Models.StationInfo>> Stations = [];

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private static readonly int Version = 2;
    private static readonly int MaxLength = 4194304;

    private bool InitComplete = false;

    [DoesNotReturn]
    private void Fail(string? message, object? extraData = null)
    {
        Logger.LogAssertFailure(message, JsonConvert.SerializeObject(extraData));

        if (Debugger.IsAttached)
        {
            Debugger.Break();
        }

        throw new BadDataException(message, extraData);
    }

    private void Assert([DoesNotReturnIf(false)] bool condition, [CallerArgumentExpression(nameof(condition))] string? message = null, object? extraData = null)
    {
        if (!condition)
        {
            Fail(message, extraData);
        }
    }

    protected async Task InitAsync()
    {
        if (InitComplete) return;

        await Init_OverridesAsync();

        Init_Systems();

        Init_Bodies();

        using var ctx = ContextFactory.CreateDbContext();

        if (Files.Count == 0)
        {
            Logger.LogLoadingFileInfo();

            foreach (var file in ctx.Set<Models.FileInfo>().AsNoTracking())
            {
                Files[file.FileName] = file;
            }
        }

        if (Software.Count == 0)
        {
            Logger.LogLoadingSoftwareVersions();

            foreach (var sw in ctx.Set<Models.SoftwareInfo>().AsNoTracking())
            {
                Software[(sw.SoftwareName, sw.SoftwareVersion)] = sw;
            }
        }

        if (GameVersions.Count == 0)
        {
            Logger.LogLoadingGameVersions();

            foreach (var gv in ctx.Set<Models.GameVersionInfo>().AsNoTracking())
            {
                GameVersions[(gv.GameVersion, gv.GameBuild, gv.IsOdyssey, gv.IsHorizons)] = gv;
            }
        }

        if (Signals.Count == 0)
        {
            Logger.LogLoadingSignals();
            foreach (var s in ctx.Set<Models.SignalInfo>().AsNoTracking())
            {
                Signals[(s.SignalName, s.SignalType, s.IsStation)] = s;
                SignalsById[s.Id] = s;
            }
        }

        if (SchemaEvents.Count == 0)
        {
            Logger.LogLoadingSchemaEvents();
            foreach (var s in ctx.Set<Models.SchemaEventInfo>().AsNoTracking())
            {
                SchemaEvents[(s.Schema, s.EventType)] = s;
            }
        }

        if (BodySignals.Count == 0)
        {
            Logger.LogLoadingBodySignals();

            foreach (var s in ctx.Set<Models.BodySignalInfo>().AsNoTracking())
            {
                BodySignals[(s.SignalType, s.SignalCount, s.Category, s.SubCategory, s.Region, s.EntryID)] = s;
            }
        }

        if (Stations.Count == 0)
        {
            Logger.LogLoadingStations();

            foreach (var s in ctx.Set<Models.StationInfo>().AsNoTracking())
            {
                if (!Stations.TryGetValue((s.StationName, s.MarketId, s.StationType, s.SystemName, s.SystemAddress, s.BodyName), out var stnlist))
                {
                    Stations[(s.StationName, s.MarketId, s.StationType, s.SystemName, s.SystemAddress, s.BodyName)] = stnlist = [];
                }

                stnlist.Add(s);
            }
        }

        if (SignalInfoSetCounts.Count == 0)
        {
            Logger.LogLoadingSignalCounts();

            foreach (var s in ctx.Set<Models.SignalInfoSet>().Select(e => new { e.Id, e.SignalCount }))
            {
                SignalInfoSetCounts[s.Id] = s.SignalCount;
            }
        }

        InitComplete = true;
    }

    protected Models.SoftwareInfo GetOrAddSoftware(string softwareName, string softwareVersion)
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

    protected Models.GameVersionInfo GetOrAddGameVersion(string? gamebuild, string? gameversion, bool? isOdyssey, bool? isHorizons)
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

    protected Models.StationInfo GetOrAddStation(string? stationName, long? marketId, string? stationType, string? systemName, long? systemAddress, string? bodyName, decimal? latitude, decimal? longitude)
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

        if (stnlist.FirstOrDefault(e => e.Latitude == latitude && e.Longitude == longitude) is { } stnExact)
        {
            return stnExact;
        }

        if (stnlist.FirstOrDefault(e => e.Latitude > latitude - 0.0001m
                                     && e.Latitude < latitude + 0.0001m
                                     && e.Longitude > longitude - 0.0001m
                                     && e.Longitude < longitude + 0.0001m) is { } stn)
        {
            return stn;
        }

        using var ctx = ContextFactory.CreateDbContext();

        var station = new Models.StationInfo
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

    protected Models.SignalInfo GetOrAddSignal(string name, string? type, bool? isStation)
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

    protected Models.SchemaEventInfo GetOrAddSchemaEvent(string schema, string? eventType)
    {
        if (SchemaEvents.TryGetValue((schema, eventType), out var schemaEvent))
        {
            return schemaEvent;
        }

        using var ctx = ContextFactory.CreateDbContext();
        schemaEvent = new Models.SchemaEventInfo
        {
            Schema = schema,
            EventType = eventType
        };

        ctx.Add(schemaEvent);
        ctx.SaveChanges();

        SchemaEvents[(schema, eventType)] = schemaEvent;
        return schemaEvent;
    }

    protected Models.SignalInfoSet GetOrAddSignalInfoSet(ICollection<Models.SignalInfo> signals, Models.SystemInfo? system)
    {
        var signalIds = signals.Select(e => e.Id).Order().ToList();
        var signalIdsJson =
            JsonConvert.SerializeObject(
                signalIds
                    .GroupBy(e => e)
                    .Select(g => g.Count() == 1 ? (object)g.Key : new[] { g.Key, g.Count() })
            );

        if (SignalInfoSetCache.TryGetValue((signalIdsJson, system?.SystemNameId, system?.ModSystemAddress, system?.X, system?.Y, system?.Z), out var signalSet))
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
               .FirstOrDefault(e => e.FirstSignalId == firstSigId
                                 && e.LastSignalId == lastSigId
                                 && e.SignalCount == signalCount
                                 && e.SignalSetJson == signalIdsJson
                                 && ((system == null && e.SystemId == null) || e.SystemId == system!.Id));

        if (signalSet != null)
        {
            if (!SignalInfoSetCacheById.TryGetValue(signalSet.Id, out var byid))
            {
                SignalInfoSetCacheById[signalSet.Id] = byid = signalSet;
                SignalInfoSetCounts[signalSet.Id] = signalSet.SignalCount;
            }

            SignalInfoSetCache[(signalIdsJson, system?.SystemNameId, system?.ModSystemAddress, system?.X, system?.Y, system?.Z)] = byid;
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
                Count = g.Count(),
                System = system
            })],
            System = system
        };

        SignalInfoSetCache[(signalIdsJson, system?.SystemNameId, system?.ModSystemAddress, system?.X, system?.Y, system?.Z)] = signalSet;
        return signalSet;
    }

    protected Models.BodySignalInfo GetOrAddBodySignal(string type, int? count, string? category = null, string? subcategory = null, string? region = null, long? entryId = null)
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

    public async Task ProcessDirectoriesAsync(IEnumerable<string> dirnames)
    {
        foreach (var filename in dirnames
                                    .SelectMany<string, string>(f => [
                                        .. _fileSystem.Directory.EnumerateFiles(f, "*.jsonl.bz2", SearchOption.AllDirectories),
                                        .. _fileSystem.Directory.EnumerateFiles(f, "*.jsonl", SearchOption.AllDirectories)
                                    ])
                                    .Select(e => (Parts: _fileSystem.Path.GetFileNameWithoutExtension(e).Split("-"), Name: e))
                                    .Where(e => e.Parts.Length > 3)
                                    .OrderBy(e => e.Parts[^3])
                                    .ThenBy(e => e.Parts[^2])
                                    .ThenBy(e => e.Parts[^1])
                                    .Select(e => e.Name))
        {
            await ProcessFileAsync(filename);
        }
    }

    protected void FillCacheForFile(int fileid)
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

        foreach (var grp in BodyInfoCache.Keys.GroupBy(e => (e.FileId, e.LineNo)))
        {
            BodyInfoCounts[grp.Key] = grp.Count();
        }

        foreach (var line in ctx.Set<Models.FileLineStation>().Where(e => e.FileId == fileid).AsNoTracking())
        {
            StationInfoCache[(line.FileId, line.LineNo)] = line;
        }

        foreach (var line in ctx.Set<Models.FileLineNavRoute>().Where(e => e.FileId == fileid).AsNoTracking())
        {
            NavRouteCache[(line.FileId, line.LineNo, line.EntryNum)] = line;
        }

        foreach (var grp in NavRouteCache.Keys.GroupBy(e => (e.FileId, e.LineNo)))
        {
            NavRouteCounts[grp.Key] = grp.Count();
        }

        foreach (var line in ctx.Set<Models.FileLineSignal>().Where(e => e.FileId == fileid).AsNoTracking())
        {
            SignalInfoCache[(line.FileId, line.LineNo)] = line;

            if (SignalInfoSetCounts.TryGetValue(line.SignalSetId, out var count))
            {
                SignalInfoCounts[(line.FileId, line.LineNo)] = count;
            }
        }

        foreach (var line in ctx.Set<Models.FileLineBodySignal>().Where(e => e.FileId == fileid).AsNoTracking())
        {
            BodySignalInfoCache[(line.FileId, line.LineNo, line.EntryNum)] = line;
        }

        foreach (var grp in BodySignalInfoCache.Keys.GroupBy(e => (e.FileId, e.LineNo)))
        {
            BodySignalInfoCounts[grp.Key] = grp.Count();
        }
    }

    protected void WriteIndexedFile(string filepath, string indexFilename, int? lineCount, bool force)
    {
        Logger.LogWritingIndexedFile(indexFilename);

        if (_fileSystem.File.Exists(indexFilename)
            && _fileSystem.File.Exists(indexFilename + ".index")
            && lineCount is int lineCountVal
            && lineCountVal > 0
            && !force
            && _fileSystem.FileInfo.New(indexFilename + ".index") is { } ixInfo
            && ixInfo.Length % 8 == 0
            && ((lineCountVal + 1023) / 1024) <= (ixInfo.Length / 8) - 1)
        {
            using var indexStream = _fileSystem.File.Open(indexFilename + ".index", FileMode.Open, FileAccess.Read, FileShare.Read);
            indexStream.Seek(indexStream.Length - 16, SeekOrigin.Begin);
            var ixlineno = (indexStream.Position / 8) * 1024;
            long startPos = 0;
            long endPos = 0;
            Span<byte> ixStartEndData = stackalloc byte[16];
            indexStream.ReadExactly(ixStartEndData);
            startPos = BinaryPrimitives.ReadInt64LittleEndian(ixStartEndData);
            endPos = BinaryPrimitives.ReadInt64LittleEndian(ixStartEndData[8..]);

            if (endPos > startPos && endPos - startPos < 1048576)
            {
                using var ixbzStream = _fileSystem.File.Open(indexFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
                using (var ixmemStream = new MemoryStream())
                {
                    var buf = ArrayPool<byte>.Shared.Rent((int)(endPos - startPos));
                    ixbzStream.Seek(startPos, SeekOrigin.Begin);
                    ixbzStream.ReadExactly(buf.AsSpan(0, (int)(endPos - startPos)));
                    ixmemStream.Write(buf.AsSpan(0, (int)(endPos - startPos)));
                    ixmemStream.Seek(0, SeekOrigin.Begin);
                    ArrayPool<byte>.Shared.Return(buf);

                    using var ixStream = new BZip2InputStream(ixmemStream);
                    using var ixReader = new EventReader(ixStream);

                    while (ixReader.TryReadLine(out _))
                    {
                        ixlineno++;
                    }
                }

                if (ixlineno >= lineCount)
                {
                    return;
                }
            }
        }

        Stream stream = _fileSystem.File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        if (filepath.EndsWith(".bz2"))
        {
            stream = new BZip2InputStream(stream);
        }

        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(indexFilename)!);
        using (var rawFileStream = _fileSystem.File.Open(indexFilename + ".tmp", FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            using var rawFileIndexStream = _fileSystem.File.Open(indexFilename + ".index.tmp", FileMode.Create, FileAccess.Write, FileShare.Read);
            using var memStream = new MemoryStream();
            var bz2stream = new BZip2OutputStream(memStream, true);
            Span<byte> idxspan = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(idxspan, 0);
            rawFileIndexStream.Write(idxspan);

            using var reader = new EventReader(stream);

            int lineno = 0;

            while (reader.TryReadLine(out var line))
            {
                if (line.Length > 1)
                {
                    var jsonReader = new Utf8JsonReader(line);
                    Debug.Assert(jsonReader.Read());
                    Debug.Assert(jsonReader.TrySkip());
                }

                var pos = line.Start;

                while (line.TryGet(ref pos, out var mem, true))
                {
                    bz2stream.Write(mem.Span);
                }

                lineno++;

                if ((lineno % 1024) == 0)
                {
                    bz2stream.Dispose();
                    memStream.Seek(0, SeekOrigin.Begin);
                    memStream.CopyTo(rawFileStream);
                    memStream.Seek(0, SeekOrigin.Begin);
                    memStream.SetLength(0);

                    bz2stream = new BZip2OutputStream(memStream, true);

                    BinaryPrimitives.WriteInt64LittleEndian(idxspan, rawFileStream.Position);
                    rawFileIndexStream.Write(idxspan);

                    Console.Error.Write(".");
                    Console.Error.Flush();

                    if ((lineno % 65536) == 0)
                    {
                        Console.Error.WriteLine($" {lineno}");
                    }
                }
            }

            Console.Error.WriteLine($" {lineno}");

            if ((lineno % 1024) != 0)
            {
                bz2stream.Dispose();
                memStream.Seek(0, SeekOrigin.Begin);
                memStream.CopyTo(rawFileStream);

                BinaryPrimitives.WriteInt64LittleEndian(idxspan, rawFileStream.Position);
                rawFileIndexStream.Write(idxspan);
            }
        }

        _fileSystem.File.Move(indexFilename + ".tmp", indexFilename, true);
        _fileSystem.File.Move(indexFilename + ".index.tmp", indexFilename + ".index", true);
    }

    public async Task ProcessFileAsync(string filepath)
    {
        await InitAsync();

        if (!_fileSystem.File.Exists(filepath))
        {
            return;
        }

        var fileinfo = _fileSystem.FileInfo.New(filepath);
        var filelen = fileinfo.Length;

        await ProcessFileAsync(filepath, filelen);
    }

    protected async Task<Models.FileInfo> GetOrAddFileAsync(string filepath)
    {
        var filename = _fileSystem.Path.GetFileName(filepath);

        if (!filename.EndsWith(".bz2"))
        {
            filename += ".bz2";
        }

        bool test = false;

        if (_fileSystem.Path.GetDirectoryName(filepath) is string filedir && _fileSystem.Path.GetFileName(filedir) is string lastdir)
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
                || dayext.Split('.', 2) is not [string daystr, _]
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

            Models.SchemaEventInfo? primarySchemaEvent = null;

            if (primarySchema?.PrimarySchema != null)
            {
                primarySchemaEvent = GetOrAddSchemaEvent(primarySchema.PrimarySchema, primarySchema.EventType ?? eventType);
            }

            try
            {
                using var ctx = ContextFactory.CreateDbContext();

                file = new Models.FileInfo
                {
                    FileName = filename,
                    Date = date,
                    PrimarySchema = primarySchema?.PrimarySchema,
                    EventType = primarySchema?.EventType ?? eventType,
                    IsTest = primarySchema?.IsTest == true || test,
                    PrimarySchemaEventId = primarySchemaEvent?.Id
                };

                ctx.Add(file);
                ctx.SaveChanges();
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                using var ctx = ContextFactory.CreateDbContext();
                file = ctx.Set<Models.FileInfo>().First(e => e.FileName == filename);
            }

            Files[filename] = file;
        }

        return file;
    }

    protected async Task ProcessFileAsync(string filepath, long filelen)
    {
        var file = await GetOrAddFileAsync(filepath);

        if (file.CompressedSize == filelen
            && file.UncompressedSize != null
            && file.LineCount != null
            && file.ErrorCount == 0
            && file.ProcessedVersion == Version
            && Settings.Reprocess != true)
        {
            return;
        }

        var context = new FileProcessingContext(filepath, filelen, file, _fileSystem.Path);

        if (Settings.IndexedDir != null)
        {
            WriteIndexedFile(
                context.FilePath,
                _fileSystem.Path.Join(Settings.IndexedDir, context.IndexedFilename),
                context.File.LineCount,
                context.FileLength > context.File.UncompressedSize
            );
        }

        Logger.LogProcessingFile(context.File.FileName);
        Logger.LogProcessingFileState(
            context.File.CompressedSize,
            context.File.UncompressedSize,
            context.File.LineCount,
            context.File.ErrorCount,
            context.File.ProcessedVersion,
            context.FileLength,
            Version
        );

        FillCacheForFile(context.File.Id);

        Stream stream = _fileSystem.File.Open(context.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        if (context.FilePath.EndsWith(".bz2"))
        {
            stream = new BZip2InputStream(stream);
        }

        using var reader = new EventReader(stream);

        var data = new FileLineData();

        while (reader.TryReadLine(out var line))
        {
            context.LineCount++;

            if ((context.LineCount % 1000) == 0)
            {
                Console.Error.Write(".");
                Console.Error.Flush();

                await SaveUpdatesAsync(context);

                if ((context.LineCount % 64000) == 0)
                {
                    Console.Error.WriteLine($" {context.LineCount}");
                }
            }

            ProcessLine(line, context, ref data);
        }

        Console.Error.WriteLine($" {context.LineCount}");

        await SaveUpdatesAsync(context);

        Models.SchemaEventInfo? fileSchemaEvent = null;

        if (context.File.PrimarySchema != null)
        {
            fileSchemaEvent = GetOrAddSchemaEvent(context.File.PrimarySchema, context.File.EventType);
        }

        using (var ctx = ContextFactory.CreateDbContext())
        {
            var fileEntry = ctx.Attach(context.File);
            fileEntry.Property(e => e.LineCount).CurrentValue = context.LineCount;
            fileEntry.Property(e => e.CompressedSize).CurrentValue = context.FileLength;
            fileEntry.Property(e => e.UncompressedSize).CurrentValue = reader.Position;
            fileEntry.Property(e => e.SystemLineCount).CurrentValue = context.SystemLineCount;
            fileEntry.Property(e => e.StationLineCount).CurrentValue = context.StationLineCount;
            fileEntry.Property(e => e.BodyLineCount).CurrentValue = context.BodyLineCount;
            fileEntry.Property(e => e.NavRouteSystemCount).CurrentValue = context.NavRouteSystemCount;
            fileEntry.Property(e => e.SignalCount).CurrentValue = context.SignalCount;
            fileEntry.Property(e => e.BodySignalCount).CurrentValue = context.BodySignalCount;
            fileEntry.Property(e => e.ErrorCount).CurrentValue = context.ErrorCount;
            fileEntry.Property(e => e.ProcessedVersion).CurrentValue = Version;
            fileEntry.Property(e => e.PrimarySchemaEventId).CurrentValue = fileSchemaEvent?.Id;

            ctx.SaveChanges();
        }

        if (Settings.IndexedDir != null && !context.FilePath.EndsWith(".bz2") && reader.Position > context.FileLength)
        {
            WriteIndexedFile(
                context.FilePath,
                _fileSystem.Path.Join(Settings.IndexedDir, context.IndexedFilename),
                null,
                true
            );
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
        NavRouteCounts.Clear();
        SignalInfoCache.Clear();
        SignalInfoCounts.Clear();
        BodySignalInfoCache.Clear();
        BodySignalInfoCounts.Clear();
    }

    protected void ProcessLine(
            ReadOnlySequence<byte> line,
            FileProcessingContext context,
            ref FileLineData data
        )
    {
        if (LineInfoCache.TryGetValue((context.File.Id, context.LineCount), out var lineInfo)
            && lineInfo.ProcessedVersion == Version
            && lineInfo.SchemaEventId != null
            && lineInfo.HasStation is bool hasStation
            && lineInfo.HasBody is bool hasBody
            && lineInfo.NavRouteSystemCount is int lineNavRouteSystemCount
            && lineInfo.BodySignalCount is int lineBodySignalCount
            && lineInfo.SignalCount is int lineSignalCount
            && BodyInfoCache.ContainsKey((lineInfo.FileId, lineInfo.LineNo, 0)) == hasBody
            && StationInfoCache.ContainsKey((lineInfo.FileId, lineInfo.LineNo)) == hasStation
            && BodySignalInfoCounts.GetValueOrDefault((lineInfo.FileId, lineInfo.LineNo)) == lineBodySignalCount
            && SignalInfoCounts.GetValueOrDefault((lineInfo.FileId, lineInfo.LineNo)) == lineSignalCount
            && NavRouteCounts.GetValueOrDefault((lineInfo.FileId, lineInfo.LineNo)) == lineNavRouteSystemCount)
        {
            context.SystemLineCount += lineInfo.SystemId != null ? 1 : 0;
            context.BodyLineCount += hasBody ? 1 : 0;
            context.StationLineCount += hasStation ? 1 : 0;
            context.NavRouteSystemCount += lineNavRouteSystemCount;
            context.SignalCount += lineSignalCount;
            context.BodySignalCount += lineBodySignalCount;

            return;
        }

        data.Clear(context.File, context.LineCount, int.CreateSaturating(line.Length));

        if (line.Length < 2 || line.Length >= MaxLength)
        {
            data.IsBad = true;
        }
        else
        {
            try
            {
                if (!TryProcessLine(line, ref data))
                {
                    Logger.LogIncompleteMessage(context.FilePath, context.LineCount);

                    if (Settings.BreakOnBadData != false && Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }

                    if (Settings.ExitOnBadData != false)
                    {
                        Environment.Exit(1);
                    }

                    data.IsBad = true;
                    context.ErrorCount++;
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                HandleBadData(context.FilePath, context.LineCount, ex);

                data.IsBad = true;
                context.ErrorCount++;
            }
            catch (BadDataException ex)
            {
                HandleBadData(context.FilePath, context.LineCount, ex);

                data.IsBad = true;
                context.ErrorCount++;
            }

            if (data.System == null
                && data.Body == null
                && data.Station == null
                && data.Signals.Count == 0
                && data.BodySignals.Count == 0
                && data.NavRouteSystems.Count == 0
                && data.EventType != "NavRoute")
            {
                Logger.LogNoDataAvailable(context.FilePath, context.LineCount);

                if (Settings.BreakOnBadData != false && Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                if (Settings.ExitOnBadData != false)
                {
                    Environment.Exit(1);
                }
            }
        }

        FillLines(data, context);

        context.SystemLineCount += data.System != null ? 1 : 0;
        context.BodyLineCount += data.Body != null ? 1 : 0;
        context.StationLineCount += data.Station != null ? 1 : 0;
        context.NavRouteSystemCount += data.NavRouteSystems.Count;
        context.SignalCount += data.Signals.Count;
        context.BodySignalCount += data.BodySignals.Count;
    }

    protected void FillLines(
            FileLineData data,
            FileProcessingContext context
        )
    {
        context.NewLines[data.LineNo] = new Models.FileLineInfo
        {
            FileId = context.File.Id,
            LineNo = data.LineNo,
            LineLength = data.LineLength,
            GatewayTimestamp = data.GatewayTimestamp,
            Timestamp = data.Timestamp,
            ProcessedVersion = Version,
            GameVersion = data.GameVersionInfo,
            Software = data.Software,
            System = data.System,
            SchemaEvent = data.SchemaEvent,
            IsBad = data.IsBad,
            HasBody = data.Body != null,
            HasStation = data.Station != null,
            NavRouteSystemCount = data.NavRouteSystems.Count,
            BodySignalCount = data.BodySignals.Count,
            SignalCount = data.Signals.Count,
        };

        if (data.Body != null)
        {
            context.NewBodyLines[(data.LineNo, 0)] = new Models.FileLineBody
            {
                FileId = context.File.Id,
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
            context.NewBodyLines[(data.LineNo, entrynum)] = new Models.FileLineBody
            {
                FileId = context.File.Id,
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
            context.NewStationLines[data.LineNo] = new Models.FileLineStation
            {
                FileId = context.File.Id,
                LineNo = data.LineNo,
                GatewayTimestamp = data.GatewayTimestamp,
                Station = data.Station,
                LatitudeError = data.Latitude == data.Station.Latitude ? null : (short)Math.Round((data.Latitude - data.Station.Latitude) * 1000000 ?? 0),
                LongitudeError = data.Longitude == data.Station.Longitude ? null : (short)Math.Round((data.Longitude - data.Station.Longitude) * 1000000 ?? 0)
            };
        }

        if (data.Signals.Count != 0)
        {
            var signalSet = GetOrAddSignalInfoSet(data.Signals.Values, data.System);

            context.NewSignalEntries[data.LineNo] = new Models.FileLineSignal
            {
                FileId = context.File.Id,
                LineNo = data.LineNo,
                GatewayTimestamp = data.GatewayTimestamp,
                System = data.System,
                SignalInfoSet = signalSet
            };
        }

        foreach (var (entnum, system) in data.NavRouteSystems)
        {
            context.NewNavRouteEntries[(data.LineNo, entnum)] = new Models.FileLineNavRoute
            {
                FileId = context.File.Id,
                LineNo = data.LineNo,
                EntryNum = entnum,
                GatewayTimestamp = data.GatewayTimestamp,
                System = system
            };
        }

        foreach (var (entnum, signal) in data.BodySignals)
        {
            context.NewBodySignalEntries[(data.LineNo, entnum)] = new Models.FileLineBodySignal
            {
                FileId = context.File.Id,
                LineNo = data.LineNo,
                EntryNum = entnum,
                GatewayTimestamp = data.GatewayTimestamp,
                Latitude = data.Latitude,
                Longitude = data.Longitude,
                Signal = signal,
                Body = data.Body
            };
        }
    }

    private void HandleBadData(string filepath, int lineCount, Exception ex)
    {
        Logger.LogBadData(ex, filepath, lineCount, ex.Message);

        if (Settings.BreakOnBadData != false && Debugger.IsAttached)
        {
            Debugger.Break();
        }

        if (Settings.ExitOnBadData != false)
        {
            Environment.Exit(1);
        }
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

    protected async Task SaveUpdatesAsync(FileProcessingContext context)
    {
        await using (var ctx = await ContextFactory.CreateDbContextAsync())
        {
            ctx.AddRange(SystemCache.Values.Where(e => e.Id <= 0));
            await ctx.SaveChangesAsync();
        }

        await SaveBodiesAsync();
        await SaveSignalsAsync();

        await SaveLinesAsync(context.NewLines);

        context.NewLines.Clear();

        await SaveBodyLinesAsync(context.NewBodyLines);

        context.NewBodyLines.Clear();

        await SaveStationLinesAsync(context.NewStationLines);

        context.NewStationLines.Clear();

        await SaveNavRouteEntriesAsync(context.NewNavRouteEntries);

        context.NewNavRouteEntries.Clear();

        await SaveSignalEntriesAsync(context.NewSignalEntries);

        context.NewSignalEntries.Clear();

        await SaveBodySignalEntriesAsync(context.NewBodySignalEntries);

        context.NewBodySignalEntries.Clear();
    }

    private async Task SaveBodiesAsync()
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

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

        await ctx.SaveChangesAsync();
    }

    private async Task SaveSignalsAsync()
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

        foreach (var ent in SignalInfoSetCache.Values)
        {
            foreach (var sig in ent.SignalSetItems)
            {
                if (sig.Signal != null)
                {
                    Assert(sig.Signal.Id > 0);
                    sig.SignalInfoId = sig.Signal.Id;
                    sig.Signal = null;
                }

                if (sig.System != null)
                {
                    Assert(sig.System.Id > 0);
                    sig.SystemId = sig.System.Id;
                    sig.System = null;
                }
            }

            if (ent.System != null)
            {
                Assert(ent.System.Id > 0);
                ent.SystemId = ent.System.Id;
                ent.System = null;
            }

            if (ent.Id == 0)
            {
                ctx.Add(ent);
            }
        }

        await ctx.SaveChangesAsync();
    }

    private async Task SaveLinesAsync(Dictionary<int, Models.FileLineInfo> newLines)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

        var softwareUpdates = new Dictionary<int, (Models.SoftwareInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();
        var gameVersionUpdates = new Dictionary<int, (Models.GameVersionInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();
        var systemUpdates = new Dictionary<int, (Models.SystemInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();
        var schemaEventUpdates = new Dictionary<int, (Models.SchemaEventInfo, DateTime? FirstSeen, DateTime? LastSeen)>();
        var sectorUpdates = new Dictionary<int, (Models.Sector Sector, DateTime? FirstSeen, DateTime? LastSeen)>();

        foreach (var _ent in newLines.Values)
        {
            Assert(_ent.Software?.Id != 0);
            Assert(_ent.GameVersion?.Id != 0);
            Assert(_ent.System?.Id != 0);
            Assert(_ent.SchemaEvent?.Id != 0);

            var software = _ent.Software;
            var gameVersion = _ent.GameVersion;
            var system = _ent.System;
            var gatewayTimestamp = _ent.GatewayTimestamp;
            var schemaEvent = _ent.SchemaEvent;
            var sector = _ent.System?.SectorId is int sectorId
                       ? SectorsById.GetValueOrDefault(sectorId)
                       : _ent.System?.SectorAddress is int sectorAddr
                       ? SectorsByAddr.GetValueOrDefault(sectorAddr)
                       : null;

            var ent = _ent with
            {
                SoftwareId = software?.Id,
                GameVersionId = gameVersion?.Id,
                SystemId = system?.Id,
                SchemaEventId = schemaEvent?.Id,
                GameVersion = null,
                Software = null,
                System = null,
                SchemaEvent = null
            };

            AddOrUpdateInfo(softwareUpdates, software, gatewayTimestamp);
            AddOrUpdateInfo(gameVersionUpdates, gameVersion, gatewayTimestamp);
            AddOrUpdateInfo(systemUpdates, system, gatewayTimestamp);
            AddOrUpdateInfo(schemaEventUpdates, schemaEvent, gatewayTimestamp);
            AddOrUpdateInfo(sectorUpdates, sector, gatewayTimestamp);

            if (LineInfoCache.TryGetValue((ent.FileId, ent.LineNo), out var lineInfo))
            {
                var entry = ctx.Attach(lineInfo);
                entry.Property(e => e.SystemId).CurrentValue = ent.SystemId;
                entry.Property(e => e.GameVersionId).CurrentValue = ent.GameVersionId;
                entry.Property(e => e.SoftwareId).CurrentValue = ent.SoftwareId;
                entry.Property(e => e.SchemaEventId).CurrentValue = ent.SchemaEventId;
                entry.Property(e => e.ProcessedVersion).CurrentValue = ent.ProcessedVersion;
                entry.Property(e => e.GatewayTimestamp).CurrentValue = ent.GatewayTimestamp;
                entry.Property(e => e.Timestamp).CurrentValue = ent.Timestamp;
                entry.Property(e => e.HasBody).CurrentValue = ent.HasBody;
                entry.Property(e => e.HasStation).CurrentValue = ent.HasStation;
                entry.Property(e => e.SignalCount).CurrentValue = ent.SignalCount;
                entry.Property(e => e.BodySignalCount).CurrentValue = ent.BodySignalCount;
                entry.Property(e => e.NavRouteSystemCount).CurrentValue = ent.NavRouteSystemCount;
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

        foreach (var (info, firstSeen, lastSeen) in sectorUpdates.Values)
        {
            var entry = ctx.Attach(info);
            entry.Property(e => e.FirstSeen).CurrentValue = firstSeen;
            entry.Property(e => e.LastSeen).CurrentValue = lastSeen;
        }

        await ctx.SaveChangesAsync();
    }

    private async Task SaveBodyLinesAsync(Dictionary<(int LineNo, int EntryNum), Models.FileLineBody> newBodyLines)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

        var bodyUpdates = new Dictionary<long, (Models.BodyInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();

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

        await ctx.SaveChangesAsync();
    }

    private async Task SaveStationLinesAsync(Dictionary<int, Models.FileLineStation> newStationLines)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

        var stationUpdates = new Dictionary<int, (Models.StationInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();

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

        await ctx.SaveChangesAsync();
    }

    private async Task SaveNavRouteEntriesAsync(Dictionary<(int LineNo, int EntryNum), Models.FileLineNavRoute> newNavRouteEntries)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

        var systemUpdates = new Dictionary<int, (Models.SystemInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();

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

        await ctx.SaveChangesAsync();
    }

    private async Task SaveSignalEntriesAsync(Dictionary<int, Models.FileLineSignal> newSignalEntries)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

        var signalUpdates = new Dictionary<int, (Models.SignalInfo Info, DateTime? FirstSeen, DateTime? LastSeen)>();
        var signalItemUpdates = new Dictionary<int, (Models.SignalInfoSetItem Info, DateTime? FirstSeen, DateTime? LastSeen)>();

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

            foreach (var signalItem in siginfoset.SignalSetItems)
            {
                AddOrUpdateInfo(signalItemUpdates, signalItem, gatewayTimestamp);

                if (SignalsById.TryGetValue(signalItem.SignalInfoId, out var signal))
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

        await ctx.SaveChangesAsync();
    }

    private async Task SaveBodySignalEntriesAsync(Dictionary<(int LineNo, int EntryNum), Models.FileLineBodySignal> newBodySignalEntries)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync();

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

        await ctx.SaveChangesAsync();
    }
}
