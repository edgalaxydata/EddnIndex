using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using EddnIndex.Common;
using EddnIndexLookup.DTO;
using EddnIndexLookup.Options;
using Ionic.BZip2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Testably.Abstractions.Helpers;
using Models = EddnIndex.Common.Models;
using Sectors = EddnIndex.Common.Sectors;

namespace EddnIndexLookup.Services;

/// <summary>
/// Backend service for EDDN lookup API
/// </summary>
/// <param name="contextFactory">DbContext factory</param>
/// <param name="logger">Logger</param>
/// <param name="options">Service settings</param>
/// <param name="fileSystem">Filesystem abstraction</param>
public class EddnLookupService(
        IDbContextFactory<Models.EDDNContext> contextFactory,
        ILogger<EddnLookupService> logger,
        IOptions<EddnLookupServiceSettings> options,
        IFileSystem fileSystem
    )
{
    private readonly IDbContextFactory<Models.EDDNContext> _contextFactory = contextFactory;
    private readonly ILogger _logger = logger;
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly EddnLookupServiceSettings _settings = options.Value;
    private readonly Dictionary<string, (DateTime LastMod, long Length, Dictionary<int, LinkedListNode<(string Filename, int ChunkNo, List<string> Lines, DateTime LastUsed)>> Entries)> _lineCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<(string Filename, int ChunkNo, List<string> Lines, DateTime LastUsed)> _lineCacheLRU = [];
    private readonly Lock _lineCacheLock = new();

    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = options.Value.MaxExtractCacheSize ?? (128 * 1048576) });

    private readonly TimeSpan _maxCacheAge = TimeSpan.FromHours(1);
    private readonly int _maxCacheSize = 8192;

    private protected async IAsyncEnumerable<long> GetSystemNameIdsAsync(string? name, [EnumeratorCancellation] CancellationToken canceltoken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield break;
        }

        name = name.Trim();

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        foreach (var entry in await ctx.Set<Models.SystemName>().Where(e => e.Name == name).ToListAsync(canceltoken))
        {
            yield return -entry.Id;
        }

        if (SystemHelpers.TrySplitProcgenName(name, out string? sectorName, out int mid, out int n2, out int masscode, true)
            && n2 is >= 0 and < 65536
            && mid is >= 0 and < 0x200000
            && masscode is >= 0 and < 8)
        {
            long boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);

            foreach (var sector in ctx.Set<Models.Sector>().Where(e => e.Name == sectorName))
            {
                if (sector.SectorAddress is int sectoraddr && sectoraddr >= 0 && sectoraddr < 0x100000)
                {
                    yield return ((long)sectoraddr << 40) | boxelid;
                }

                yield return (((long)sector.Id + 0x100000) << 40) | boxelid;
            }
        }
    }

    private protected async Task<Dictionary<string, List<long>>> GetSystemNameIdsAsync(ICollection<string> names, CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var sysNames = await
            ctx.Set<Models.SystemName>()
               .Where(e => names.Contains(e.Name))
               .AsAsyncEnumerable()
               .GroupBy(e => e.Name)
               .ToDictionaryAsync(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase, canceltoken);

        var sectors = await
            ctx.Set<Models.Sector>()
               .Where(e => names.Contains(e.Name))
               .AsAsyncEnumerable()
               .GroupBy(e => e.Name)
               .ToDictionaryAsync(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase, canceltoken);

        var sysNameIds = new Dictionary<string, List<long>>(StringComparer.OrdinalIgnoreCase);

        foreach (string sysname in names)
        {
            var ids = new List<long>();

            if (SystemHelpers.TrySplitProcgenName(sysname, out string? sectorname, out int mid, out int n2, out int masscode, true)
                && n2 >= 0
                && n2 < 65536
                && mid >= 0
                && mid < 0x200000
                && masscode >= 0
                && masscode < 8
                && sectors.TryGetValue(sectorname, out var sectorEnts))
            {
                long boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);

                foreach (var sector in sectorEnts)
                {
                    if (sector.SectorAddress is int sectoraddr && sectoraddr >= 0 && sectoraddr < 0x100000)
                    {
                        ids.Add(((long)sectoraddr << 40) | boxelid);
                    }

                    ids.Add((((long)sector.Id + 0x100000) << 40) | boxelid);
                }
            }

            if (sysNames.TryGetValue(sysname, out var sysNameEnts))
            {
                foreach (var sysNameEnt in sysNameEnts)
                {
                    ids.Add(-sysNameEnt.Id);
                }
            }

            if (ids.Count > 0)
            {
                sysNameIds[sysname] = ids;
            }
        }

        return sysNameIds;
    }

    private protected async IAsyncEnumerable<(long? SystemNameId, int BodyNameId)> GetBodyNameIdsAsync(string? name, [EnumeratorCancellation] CancellationToken canceltoken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield break;
        }

        name = name.Trim();

        var nameEnts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [""] = name
        };

        for (int spacePos = name.LastIndexOf(' '); spacePos > 0; spacePos = name.LastIndexOf(' ', spacePos - 1))
        {
            nameEnts[name[spacePos..]] = name[..spacePos];
        }

        var sysNamesToIds = await GetSystemNameIdsAsync(nameEnts.Values, canceltoken);

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        await foreach (var entry in ctx.Set<Models.BodyName>().Where(e => e.Name == name).AsAsyncEnumerable())
        {
            yield return (null, entry.Id);
        }

        var desigs = await
            ctx.Set<Models.BodyDesignation>()
               .Where(e => nameEnts.Keys.Contains(e.Designation))
               .AsAsyncEnumerable()
               .GroupBy(e => e.Designation)
               .ToDictionaryAsync(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase, canceltoken);

        foreach (var (desigName, desigEnts) in desigs)
        {
            if (nameEnts.TryGetValue(desigName, out string? sysname) && sysNamesToIds.TryGetValue(sysname, out var sysNameIds))
            {
                foreach (var desigEnt in desigEnts)
                {
                    foreach (long sysNameId in sysNameIds)
                    {
                        yield return (sysNameId, -desigEnt.Id);

                        if (desigEnt.DesignationId is int desigid)
                        {
                            yield return (sysNameId, desigid);
                        }
                    }
                }
            }
        }
    }

    private protected async Task<List<int>?> GetSystemIdsAsync(string? systemName, long? systemAddress, CancellationToken canceltoken)
    {
        if (string.IsNullOrWhiteSpace(systemName) && (systemAddress == null || systemAddress <= 0))
        {
            return null;
        }

        var sysNameIds = await GetSystemNameIdsAsync(systemName, canceltoken).ToListAsync(canceltoken);
        long? modsysaddr = SystemHelpers.SystemAddressToModSystemAddress(systemAddress);

        if ((modsysaddr == null && systemAddress != null) || (sysNameIds.Count == 0 && systemName != null))
        {
            return null;
        }

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.SystemInfo> query = ctx.Set<Models.SystemInfo>();

        if (sysNameIds.Count != 0)
        {
            query = query.Where(e => e.SystemNameId != null && sysNameIds.Contains(e.SystemNameId.Value));
        }

        if (modsysaddr != null)
        {
            query = query.Where(e => e.ModSystemAddress == modsysaddr);
        }

        return await query.Select(e => e.Id).ToListAsync(canceltoken);
    }

    private protected async Task<Dictionary<int, TSystem>?> GetSystemsAsync<TSystem>(string? systemName, long? systemAddress, bool includeRejected, CancellationToken canceltoken)
        where TSystem : class, ISystemData, new()
    {
        if (string.IsNullOrWhiteSpace(systemName) && (systemAddress == null || systemAddress <= 0))
        {
            return null;
        }

        var sysNameIds = await GetSystemNameIdsAsync(systemName, canceltoken).ToListAsync(canceltoken);
        long? modsysaddr = SystemHelpers.SystemAddressToModSystemAddress(systemAddress);

        if ((modsysaddr == null && systemAddress != null) || (sysNameIds.Count == 0 && systemName != null))
        {
            return null;
        }

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.SystemInfo> query = ctx.Set<Models.SystemInfo>();

        if (sysNameIds.Count != 0)
        {
            query = query.Where(e => e.SystemNameId != null && sysNameIds.Contains(e.SystemNameId.Value));
        }

        if (modsysaddr != null)
        {
            query = query.Where(e => e.ModSystemAddress == modsysaddr);
        }

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        var systems = await
            query
                .OrderByDescending(e => e.LastSeen)
                .ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);

        return await FillSystemsAsync<TSystem>(ctx, systems, canceltoken);
    }

    private protected static TSystem? FillSystem<TSystem>(
            Models.SystemInfo system,
            Dictionary<long, string> systemNames,
            Dictionary<int, string> sectorsById,
            Dictionary<int, string> sectorsByAddr
        )
        where TSystem : class, ISystemData, new()
    {
        int systemId = system.Id;

        string? name = system switch
        {
            { SystemNameId: long sysNameId }
                when systemNames.TryGetValue(-sysNameId, out string? sysname)
                => sysname,
            { SectorId: int sectorId, PGSuffix: string pgSuffix }
                when sectorsById.TryGetValue(sectorId, out string? sectorName)
                => sectorName + pgSuffix,
            { SectorAddress: int sectorAddr, PGSuffix: string pgSuffix }
                => (sectorsByAddr.GetValueOrDefault(sectorAddr) ?? Sectors.PGSectors.GetSectorName(sectorAddr)) + pgSuffix,
            _ => null
        };

        return name == null
            ? null
            : new TSystem
            {
                Id = systemId,
                Name = name,
                SystemAddress = system.SystemAddress,
                Coords = system is { X: decimal x, Y: decimal y, Z: decimal z }
                       ? new DTO.Coords(x, y, z)
                       : null,
                FirstSeen = system.FirstSeen,
                IsRejected = system.IsRejected,
                LastSeen = system.LastSeen,
                PGName = system is { SysAddr_SectorAddress: int sa_sectorAddr, SysAddr_PGSuffix: string sa_pgsuffix }
                       ? (sectorsByAddr.GetValueOrDefault(sa_sectorAddr) ?? Sectors.PGSectors.GetSectorName(sa_sectorAddr)) + sa_pgsuffix
                       : null,
                ValidFrom = system.ValidFrom,
                ValidTo = system.ValidTo
            };
    }

    private protected static async Task<Dictionary<int, TSystem>> FillSystemsAsync<TSystem>(Models.EDDNContext ctx, Dictionary<int, Models.SystemInfo> systems, CancellationToken canceltoken)
        where TSystem : class, ISystemData, new()
    {
        var systemNameIds = systems.Values.Select(e => e.SystemNameId).Distinct().ToList();
        var sectorIds =
            systemNameIds
                .Where(e => e > 0)
                .Select(e => (int)(e!.Value >> 40))
                .Union(systems.Values.Select(e => e.SysAddr_SectorAddress).OfType<int>())
                .Distinct()
                .ToList();

        var systemNames = await
            ctx.Set<Models.SystemName>()
               .Where(e => systemNameIds.Contains(-e.Id))
               .ToDictionaryAsync(e => (long)e.Id, e => e.Name, cancellationToken: canceltoken);

        var sectorsById = await
            ctx.Set<Models.Sector>()
               .Where(e => sectorIds.Contains(e.Id + 0x100000))
               .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken: canceltoken);

        var sectorsByAddr = await
            ctx.Set<Models.Sector>()
               .Where(e => e.SectorAddress != null && sectorIds.Contains(e.SectorAddress.Value))
               .ToDictionaryAsync(e => e.SectorAddress!.Value, e => e.Name, cancellationToken: canceltoken);

        var systemDatas = new Dictionary<int, TSystem>();

        foreach (var (systemId, system) in systems)
        {
            if (FillSystem<TSystem>(system, systemNames, sectorsById, sectorsByAddr) is { } entry)
            {
                systemDatas[systemId] = entry;
            }
        }

        return systemDatas;
    }

    private protected static async Task<Dictionary<long, TBodyData>> FillBodiesAsync<TBodyData>(
            Models.EDDNContext ctx,
            Dictionary<long, Models.BodyInfo> bodies,
            CancellationToken canceltoken
        )
        where TBodyData : class, IBodyData, new()
    {
        var bodyNameIds =
            bodies
                .Values
                .Select(e => e.BodyNameId)
                .OfType<int>()
                .Union(bodies.Values.Select(e => e.BodyDesignationId).OfType<int>())
                .Distinct()
                .ToList();

        var bodyDesigIds =
            bodies
                .Values
                .Select(e => e.BodyDesignationId)
                .OfType<int>()
                .Distinct()
                .ToList();

        var systemNameIds =
            bodies
                .Values
                .Select(e => e.SystemNameId)
                .OfType<long>()
                .Union(bodies.Values.Select(e => e.System?.SystemNameId).OfType<long>())
                .Distinct()
                .ToList();

        var sectorIds =
            systemNameIds
                .Where(e => e > 0)
                .Select(e => (int)(e >> 40))
                .Distinct()
                .ToList();

        var bodyNames = await
            ctx.Set<Models.BodyName>()
               .Where(e => bodyNameIds.Contains(e.Id))
               .ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);

        var bodyDesigsById = await
            ctx.Set<Models.BodyDesignation>()
               .Where(e => bodyDesigIds.Contains(e.Id))
               .ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);

        var bodyDesigsByDesigId = await
            ctx.Set<Models.BodyDesignation>()
               .Where(e => e.DesignationId != null && bodyNameIds.Contains(e.DesignationId.Value))
               .ToDictionaryAsync(e => e.DesignationId!.Value, cancellationToken: canceltoken);

        var systemNames = await
            ctx.Set<Models.SystemName>()
               .Where(e => systemNameIds.Contains(-e.Id))
               .ToDictionaryAsync(e => (long)e.Id, e => e.Name, cancellationToken: canceltoken);

        var sectorsById = await
            ctx.Set<Models.Sector>()
               .Where(e => sectorIds.Contains(e.Id + 0x100000))
               .ToDictionaryAsync(e => (long)e.Id + 0x100000, e => e.Name, cancellationToken: canceltoken);

        var sectorsByAddr = await
            ctx.Set<Models.Sector>()
               .Where(e => e.SectorAddress != null && sectorIds.Contains(e.SectorAddress.Value))
               .ToDictionaryAsync(e => e.SectorAddress!.Value, e => e.Name, cancellationToken: canceltoken);

        var bodiesData = new Dictionary<long, TBodyData>();

        foreach (var (id, body) in bodies)
        {
            string? sysname = body switch
            {
                { SystemNameId: long sysNameId }
                    when systemNames.TryGetValue(-sysNameId, out string? sn)
                    => sn,
                { SysName_SectorId: int sectorId, SysName_PGSuffix: string pgSuffix }
                    when sectorsById.TryGetValue(sectorId, out string? sectorName)
                    => sectorName + pgSuffix,
                { SysName_SectorAddress: int sectorAddr, SysName_PGSuffix: string pgSuffix }
                    => (sectorsByAddr.GetValueOrDefault(sectorAddr) ?? Sectors.PGSectors.GetSectorName(sectorAddr)) + pgSuffix,
                _ => null
            };

            string? desigSysName = body.System switch
            {
                { SystemNameId: long sysNameId }
                    when systemNames.TryGetValue(-sysNameId, out string? sn)
                    => sn,
                { SectorId: int sectorId, PGSuffix: string pgSuffix }
                    when sectorsById.TryGetValue(sectorId, out string? sectorName)
                    => sectorName + pgSuffix,
                { SectorAddress: int sectorAddr, PGSuffix: string pgSuffix }
                    => (sectorsByAddr.GetValueOrDefault(sectorAddr) ?? Sectors.PGSectors.GetSectorName(sectorAddr)) + pgSuffix,
                _ => null
            };

            if (body.BodyNameId is int bodyNameId)
            {
                string? name = (sysname, bodyNameId) switch
                {
                    (_, > 0) when bodyNames.TryGetValue(bodyNameId, out var bn) => bn.Name,
                    (not null, _) when bodyDesigsByDesigId.TryGetValue(bodyNameId, out var bd) => sysname + bd.Designation,
                    _ => null
                };

                (string? desig, string? desigType, var desigData) =
                    body.BodyDesignationId is not int bodyDesigId
                    ? (null, null, null)
                    : (desigSysName, bodyDesigId) switch
                    {
                        (not null, > 0) when bodyDesigsById.TryGetValue(bodyDesigId, out var bd) => (desigSysName + bd.Designation, bd.DesignationType.ToString(), bd),
                        (not null, _) when bodyDesigsByDesigId.TryGetValue(bodyDesigId, out var bd) => (desigSysName + bd.Designation, bd.DesignationType.ToString(), bd),
                        _ => (null, null, null)
                    };

                if (name != null)
                {
                    bodiesData[id] = new TBodyData
                    {
                        Name = name,
                        Designation = desig,
                        Id = body.Id,
                        SystemId = body.SystemId,
                        SystemAddress = body.System?.SystemAddress,
                        ArgOfPeriapsis = body.ArgOfPeriapsis,
                        SemiMajorAxis = body.SemiMajorAxis * (decimal)Math.Pow(10, body.SemiMajorAxisScale),
                        BodyId = body.BodyId,
                        FirstSeen = body.FirstSeen,
                        Inclination = body.Inclination,
                        IsRejected = body.IsRejected,
                        LastSeen = body.LastSeen,
                        ValidFrom = body.ValidFrom,
                        ValidTo = body.ValidTo,
                        Parents = body.ParentSet?.ParentJson is string parentJson
                                ? JsonConvert.DeserializeObject<List<Dictionary<string, int>>>(parentJson)
                                : null,
                        BodyType = body.ParentSet?.BodyType,
                        DesignationType = desigType,
                        BodyDesignation = desigData
                    };
                }
            }
        }

        return bodiesData;
    }

    private protected async Task<Dictionary<long, BodyData>?> GetBodiesAsync(string? systemName, long? systemAddress, string? bodyName, int? bodyId, bool includeRejected, CancellationToken canceltoken)
    {
        var systems = await GetSystemsAsync<BodySystem>(systemName, systemAddress, includeRejected, canceltoken);

        if ((systems == null || bodyId == null || bodyId < 0) && string.IsNullOrWhiteSpace(bodyName))
        {
            return null;
        }

        var sysAndBodyNameIds = await GetBodyNameIdsAsync(bodyName, canceltoken).ToListAsync(canceltoken);
        var sysNameIds = sysAndBodyNameIds.Select(e => e.SystemNameId).OfType<long>().ToList();
        var bodyNameIds = sysAndBodyNameIds.Where(e => e.SystemNameId == null).Select(e => e.BodyNameId).ToList();
        var bodyDesigIds = sysAndBodyNameIds.Where(e => e.SystemNameId != null).Select(e => e.BodyNameId).ToList();

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.BodyInfo> query =
            ctx.Set<Models.BodyInfo>()
               .Include(e => e.ParentSet)
               .Include(e => e.System);

        if (bodyName != null)
        {
            query = query.Where(e => bodyNameIds.Contains(e.BodyNameId)
                                  || (sysNameIds.Contains(e.SystemNameId!.Value) && bodyDesigIds.Contains(e.BodyNameId)));
        }

        if (systems != null && bodyId != null)
        {
            query = query.Where(e => systems.Keys.Contains(e.SystemId) && e.BodyId == bodyId);
        }

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        var bodies = await
            query
                .OrderByDescending(e => e.LastSeen)
                .ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);

        return await FillBodiesAsync<BodyData>(ctx, bodies, canceltoken);
    }

    private protected async Task<Dictionary<int, TSystem>> GetSystemsAsync<TSystem>(ICollection<int> systemIds, bool includeRejected, CancellationToken canceltoken)
        where TSystem : class, ISystemData, new()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var query =
            ctx.Set<Models.SystemInfo>()
               .Where(e => systemIds.Contains(e.Id));

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        return await FillSystemsAsync<TSystem>(ctx, await query.ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken), canceltoken);
    }

    private protected async Task<Dictionary<int, Dictionary<long, TBodyData>>> GetSystemBodiesAsync<TBodyData>(
            ICollection<int> systemIds,
            bool includeRejected,
            CancellationToken canceltoken
        )
        where TBodyData : class, IBodyData, new()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var query =
            ctx.Set<Models.BodyInfo>()
               .Include(e => e.ParentSet)
               .Include(e => e.System)
               .Where(e => systemIds.Contains(e.SystemId));

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        var bodies = await query.ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);

        var bodies2 = await FillBodiesAsync<TBodyData>(ctx, bodies, canceltoken);

        return
            bodies2
                .Values
                .GroupBy(e => e.SystemId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(e => e.Id));
    }

    private protected async Task<Dictionary<int, StationData>> GetStationsAsync(ICollection<int> stationIds, bool includeRejected, CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var query = ctx.Set<Models.StationInfo>().Where(e => stationIds.Contains(e.Id));

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        return await
            query
               .Select(e => new DTO.StationData
               {
                   Id = e.Id,
                   SystemAddress = e.SystemAddress,
                   BodyName = e.BodyName,
                   FirstSeen = e.FirstSeen,
                   IsRejected = e.IsRejected,
                   LastSeen = e.LastSeen,
                   Latitude = e.Latitude,
                   Longitude = e.Longitude,
                   MarketId = e.MarketId,
                   StationName = e.StationName,
                   StationType = e.StationType,
                   SystemName = e.SystemName,
                   ValidFrom = e.ValidFrom,
                   ValidTo = e.ValidTo
               })
               .OrderByDescending(e => e.LastSeen)
               .ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);
    }

    private protected async Task<Dictionary<TKey, List<MatchEntry>>> FillMatchSystemData<TKey>(
            Dictionary<TKey, List<MatchEntry>> matches,
            CancellationToken canceltoken
        )
        where TKey : notnull
    {
        var systemIds = matches.Values.SelectMany(e => e).Select(e => e.SystemId).OfType<int>().ToList();

        var systems = await GetSystemsAsync<SystemData>(systemIds, true, canceltoken);

        return matches.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                      .Select(e => e.SystemId is not int sysid
                                || systems.GetValueOrDefault(sysid) is not { } sys
                                ? e
                                : e with
                                {
                                    SystemName = sys.Name,
                                    SystemAddress = sys.SystemAddress
                                }
                      )
                      .ToList()
        );
    }

    private protected async Task<Dictionary<int, List<MatchEntry>>> GetSystemMatchEntriesAsync(
            ICollection<int> systemIds,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var matches = new Dictionary<int, List<MatchEntry>>();

        foreach (int sysid in systemIds)
        {
            var routeQueryResults = await
                ctx.QuerySystemRouteMatchLines(sysid, minDate, maxDate, limitMatches)
                   .Select(e => new MatchEntry
                   {
                       FileName = e.File.FileName,
                       LineNo = e.RouteEntry.LineNo,
                       EntryNum = e.RouteEntry.EntryNum,
                       SoftwareName = e.Info.Software == null ? null : e.Info.Software.SoftwareName,
                       SoftwareVersion = e.Info.Software == null ? null : e.Info.Software.SoftwareVersion,
                       Schema = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.Schema,
                       EventType = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.EventType,
                       GameVersion = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameVersion,
                       GameBuild = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameBuild,
                       IsOdyssey = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsOdyssey,
                       IsHorizons = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsHorizons,
                       Timestamp = e.Info.Timestamp,
                       GatewayTimestamp = e.RouteEntry.GatewayTimestamp,
                       SystemId = e.RouteEntry.SystemId
                   })
                   .ToListAsync(canceltoken);

            var queryResults = await
                ctx.QuerySystemMatchLines(sysid, minDate, maxDate, limitMatches)
                   .Select(e => new MatchEntry
                   {
                       FileName = e.File.FileName,
                       LineNo = e.Info.LineNo,
                       SoftwareName = e.Info.Software == null ? null : e.Info.Software.SoftwareName,
                       SoftwareVersion = e.Info.Software == null ? null : e.Info.Software.SoftwareVersion,
                       Schema = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.Schema,
                       EventType = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.EventType,
                       GameVersion = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameVersion,
                       GameBuild = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameBuild,
                       IsOdyssey = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsOdyssey,
                       IsHorizons = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsHorizons,
                       Timestamp = e.Info.Timestamp,
                       GatewayTimestamp = e.Info.GatewayTimestamp,
                       SystemId = e.Info.SystemId,
                       BodyId = e.Body == null ? null : e.Body.BodyId,
                       StationId = e.Station == null ? null : e.Station.StationId,
                       StationName = e.Station == null || e.Station.Station == null ? null : e.Station.Station.StationName,
                       MarketId = e.Station == null || e.Station.Station == null ? null : e.Station.Station.MarketId
                   })
                   .ToListAsync(canceltoken);

            matches[sysid] = [..
                queryResults
                    .Concat(routeQueryResults)
                    .OrderByDescending(e => e.GatewayTimestamp)
                    .Take(limitMatches ?? 1000)
            ];
        }

        return matches;
    }

    private protected async Task<Dictionary<long, List<MatchEntry>>> GetBodyMatchEntriesAsync(
            ICollection<long> bodyIds,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var matches = new Dictionary<long, List<MatchEntry>>();

        foreach (long bodyid in bodyIds)
        {
            matches[bodyid] = await
                ctx.QueryBodyMatchLines(bodyid, minDate, maxDate, limitMatches)
                   .Select(e => new MatchEntry
                   {
                       FileName = e.File.FileName,
                       LineNo = e.Body.LineNo,
                       SoftwareName = e.Info.Software == null ? null : e.Info.Software.SoftwareName,
                       SoftwareVersion = e.Info.Software == null ? null : e.Info.Software.SoftwareVersion,
                       Schema = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.Schema,
                       EventType = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.EventType,
                       GameVersion = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameVersion,
                       GameBuild = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameBuild,
                       IsOdyssey = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsOdyssey,
                       IsHorizons = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsHorizons,
                       Timestamp = e.Info.Timestamp,
                       GatewayTimestamp = e.Body.GatewayTimestamp,
                       SystemId = e.Info.SystemId,
                       BodyId = e.Body.BodyId,
                       StationId = e.Station == null ? null : e.Station.StationId,
                       StationName = e.Station == null || e.Station.Station == null ? null : e.Station.Station.StationName,
                       MarketId = e.Station == null || e.Station.Station == null ? null : e.Station.Station.MarketId
                   })
                   .ToListAsync(canceltoken);
        }

        return matches;
    }

    private protected async Task<Dictionary<int, List<MatchEntry>>> GetStationMatchEntriesAsync(
            ICollection<int> stationIds,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var matches = new Dictionary<int, List<MatchEntry>>();

        foreach (int stationid in stationIds)
        {
            matches[stationid] = await
                ctx.QueryStationMatchLines(stationid, minDate, maxDate, limitMatches)
                   .Select(e => new MatchEntry
                   {
                       FileName = e.File.FileName,
                       LineNo = e.Station.LineNo,
                       SoftwareName = e.Info.Software == null ? null : e.Info.Software.SoftwareName,
                       SoftwareVersion = e.Info.Software == null ? null : e.Info.Software.SoftwareVersion,
                       Schema = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.Schema,
                       EventType = e.Info.SchemaEvent == null ? null : e.Info.SchemaEvent.EventType,
                       GameVersion = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameVersion,
                       GameBuild = e.Info.GameVersion == null ? null : e.Info.GameVersion.GameBuild,
                       IsOdyssey = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsOdyssey,
                       IsHorizons = e.Info.GameVersion == null ? null : e.Info.GameVersion.IsHorizons,
                       Timestamp = e.Info.Timestamp,
                       GatewayTimestamp = e.Station.GatewayTimestamp,
                       SystemId = e.Info.SystemId,
                       BodyId = e.Body == null ? null : e.Body.BodyId,
                       StationId = e.Station.StationId
                   })
                   .ToListAsync(canceltoken);
        }

        return await FillMatchSystemData(matches, canceltoken);
    }

    private protected async Task<Dictionary<int, List<MatchEntry>>> GetSignalMatchEntriesAsync(
            Dictionary<int, List<int>> signalSetIds,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var matches = new Dictionary<int, List<MatchEntry>>();

        foreach (var (signalId, setIds) in signalSetIds)
        {
            matches[signalId] = await
                ctx.QuerySignalMatchLines(setIds, minDate, maxDate, limitMatches)
                   .Select(e => new MatchEntry
                   {
                       FileName = e.File.FileName,
                       LineNo = e.SignalLine.LineNo,
                       SoftwareName = e.Info!.Software == null ? null : e.Info.Software.SoftwareName,
                       SoftwareVersion = e.Info!.Software == null ? null : e.Info.Software.SoftwareVersion,
                       Schema = e.Info!.SchemaEvent == null ? null : e.Info.SchemaEvent.Schema,
                       EventType = e.Info!.SchemaEvent == null ? null : e.Info.SchemaEvent.EventType,
                       GameVersion = e.Info!.GameVersion == null ? null : e.Info.GameVersion.GameVersion,
                       GameBuild = e.Info!.GameVersion == null ? null : e.Info.GameVersion.GameBuild,
                       IsOdyssey = e.Info!.GameVersion == null ? null : e.Info.GameVersion.IsOdyssey,
                       IsHorizons = e.Info!.GameVersion == null ? null : e.Info.GameVersion.IsHorizons,
                       Timestamp = e.Info!.Timestamp,
                       GatewayTimestamp = e.SignalLine.GatewayTimestamp,
                       SystemId = e.Info!.SystemId
                   })
                   .ToListAsync(canceltoken);
        }

        return await FillMatchSystemData(matches, canceltoken);
    }

    private protected async Task<Dictionary<int, int>> GetSystemMatchCountsAsync(ICollection<int> systemIds, CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        return await ctx.GetSystemMatchCountsAsync(systemIds, canceltoken);
    }

    private protected async Task<Dictionary<long, int>> GetBodyMatchCountsAsync(ICollection<long> bodyIds, CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        return await ctx.GetBodyMatchCountsAsync(bodyIds, canceltoken);
    }

    private protected async Task<Dictionary<int, int>> GetStationMatchCountsAsync(ICollection<int> stationIds, CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        return await ctx.GetStationMatchCountsAsync(stationIds, canceltoken);
    }

    private protected async Task<Dictionary<int, int>> GetSignalMatchCountsAsync(Dictionary<int, List<int>> signalSetIds, CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        return await ctx.GetSignalMatchCountsAsync(signalSetIds, canceltoken);
    }

    /// <summary>Lookup systems</summary>
    /// <remarks>
    /// Use either systemName or systemAddress to search for systems
    /// </remarks>
    /// <param name="systemName">Name of system to search for</param>
    /// <param name="systemAddress">System Address (id64) of system to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return system information</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <param name="canceltoken">Cancellation token</param>
    /// <returns>Matched system entries</returns>
    public async Task<List<SystemData>> GetSystemsAsync(
            string? systemName,
            long? systemAddress,
            bool includeRejected,
            bool brief,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        if (await GetSystemsAsync<SystemData>(systemName, systemAddress, includeRejected, canceltoken) is not { } systems)
        {
            return [];
        }

        var bodies = await GetSystemBodiesAsync<SystemBodyData>(systems.Keys, includeRejected, canceltoken);

        var bodyIds =
            bodies
                .Values
                .SelectMany(e => e)
                .Select(e => e.Key)
                .Distinct()
                .ToList();

        var systemMatchCounts = await GetSystemMatchCountsAsync(systems.Keys, canceltoken);
        var bodyMatchCounts = await GetBodyMatchCountsAsync(bodyIds, canceltoken);

        var systemMatches = brief
                          ? []
                          : await GetSystemMatchEntriesAsync(systems.Keys, limitMatches, minDate, maxDate, canceltoken);

        var bodyMatches =
            systemMatches
                .Values
                .SelectMany(e => e)
                .Where(e => e.BodyId != null)
                .GroupBy(e => e.BodyId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

        var stationMatches =
            systemMatches
                .Values
                .SelectMany(e => e)
                .Where(e => e.StationId != null)
                .GroupBy(e => e.StationId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

        var stations = await GetStationsAsync(stationMatches.Keys, includeRejected, canceltoken);

        var sysStations =
            stationMatches
                .Values
                .SelectMany(e => e)
                .Where(e => e.SystemId != null)
                .GroupBy(e => e.SystemId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.StationId)
                          .OfType<int>()
                          .Distinct()
                          .Select(stations.GetValueOrDefault)
                          .OfType<StationData>()
                          .ToList()
                );

        var bodyStations =
            stationMatches
                .Values
                .SelectMany(e => e)
                .Where(e => e.BodyId != null)
                .GroupBy(e => e.BodyId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.StationId)
                          .OfType<int>()
                          .Distinct()
                          .Select(stations.GetValueOrDefault)
                          .OfType<StationData>()
                          .ToList()
                );

        return [..
            systems
                .Values
                .Select(system => system with
                {
                    MatchCount = systemMatchCounts.GetValueOrDefault(system.Id),
                    Matches =
                        systemMatches
                            .GetValueOrDefault(system.Id)
                           ?.Select(e => e with
                           {
                               BodyName = e.BodyId is not long bodyId
                                        ? null
                                        : bodies.GetValueOrDefault(system.Id)
                                               ?.GetValueOrDefault(bodyId)
                                               ?.Name
                           })
                           .ToList(),
                    Bodies =
                        bodies
                            .GetValueOrDefault(system.Id)
                           ?.Values
                            .Select(body => body with
                            {
                                MatchCount = bodyMatchCounts.GetValueOrDefault(body.Id),
                                Matches = bodyMatches.GetValueOrDefault(body.Id),
                                Stations =
                                    bodyStations
                                        .GetValueOrDefault(body.Id)
                                       ?.Select(stn => stn with
                                        {
                                            Matches =
                                                stationMatches
                                                    .GetValueOrDefault(stn.Id)
                                                   ?.Where(e => e.BodyId == body.Id)
                                                    .Select(e => e with
                                                    {
                                                        StationName = null
                                                    })
                                                    .ToList()
                                        })
                                        .ToList()
                            })
                            .ToList(),
                    Stations =
                        sysStations
                            .GetValueOrDefault(system.Id)
                           ?.Select(stn => stn with
                            {
                                Matches =
                                    stationMatches
                                        .GetValueOrDefault(stn.Id)
                                       ?.Where(e => e.SystemId == system.Id)
                                        .Select(e => e with
                                        {
                                            StationName = null
                                        })
                                        .ToList()
                            })
                            .ToList()
                })
        ];
    }

    /// <summary>Lookup bodies</summary>
    /// <remarks>
    /// Use either bodyName or a combination of bodyId with either systemName or systemAddress to search for a body
    /// </remarks>
    /// <param name="bodyName">Name of the body to search for</param>
    /// <param name="systemName">Used with bodyId; Name of the system to search for the body</param>
    /// <param name="systemAddress">Used with bodyId; System Address (id64) of the system to search for the body</param>
    /// <param name="bodyId">Used with systemName or systemId64; Body ID of the body to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return body and system information</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <param name="canceltoken">Cancellation token</param>
    /// <returns>Matched body entries</returns>
    public async Task<List<BodyData>> GetBodiesAsync(
            string? bodyName,
            string? systemName,
            long? systemAddress,
            int? bodyId,
            bool includeRejected,
            bool brief,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        if (await GetBodiesAsync(systemName, systemAddress, bodyName, bodyId, includeRejected, canceltoken) is not { } bodies)
        {
            return [];
        }

        var systemIds =
            bodies
                .Select(e => e.Value.SystemId)
                .ToList();

        var systems = await GetSystemsAsync<BodySystem>(systemIds, includeRejected, canceltoken);

        var bodyMatchCounts = await GetBodyMatchCountsAsync(bodies.Keys, canceltoken);

        var bodyMatches = brief
                        ? []
                        : await GetBodyMatchEntriesAsync(bodies.Keys, limitMatches, minDate, maxDate, canceltoken);

        var stationMatches =
            bodyMatches
                .Values
                .SelectMany(e => e)
                .Where(e => e.StationId != null)
                .GroupBy(e => e.StationId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

        var stations = await GetStationsAsync(stationMatches.Keys, includeRejected, canceltoken);

        var bodyStations =
            stationMatches
                .Values
                .SelectMany(e => e)
                .Where(e => e.BodyId != null)
                .GroupBy(e => e.BodyId!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.StationId)
                          .OfType<int>()
                          .Distinct()
                          .Select(stations.GetValueOrDefault)
                          .OfType<StationData>()
                          .ToList()
                );

        return [..
            bodies
                .Values
                .Select(body => body with
                {
                    MatchCount = bodyMatchCounts.GetValueOrDefault(body.Id),
                    Matches = bodyMatches.GetValueOrDefault(body.Id),
                    System = systems.GetValueOrDefault(body.SystemId),
                    Stations =
                        bodyStations
                            .GetValueOrDefault(body.Id)
                            ?.Select(stn => stn with
                            {
                                Matches =
                                    stationMatches
                                        .GetValueOrDefault(stn.Id)
                                        ?.Where(e => e.BodyId == body.Id)
                                        .Select(e => e with
                                        {
                                            StationName = null
                                        })
                                        .ToList()
                            })
                            .ToList()
                })
        ];
    }

    /// <summary>Lookup stations</summary>
    /// <remarks>
    /// Use either stationName or marketId to search for a station
    /// </remarks>
    /// <param name="stationName">Name of the station to search for</param>
    /// <param name="marketId">Market ID of the station to search for</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="brief">Set brief to only return station and system information</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <param name="canceltoken">Cancellation token</param>
    /// <returns></returns>
    public async Task<List<StationData>> GetStationsAsync(
            string? stationName,
            long? marketId,
            bool includeRejected,
            bool brief,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        if (string.IsNullOrWhiteSpace(stationName) && (marketId == null || marketId <= 0))
        {
            return [];
        }

        stationName = stationName?.Trim();

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.StationInfo> query = ctx.Set<Models.StationInfo>();

        if (!string.IsNullOrWhiteSpace(stationName))
        {
            query = query.Where(e => e.StationName == stationName);
        }

        if (marketId is not null and > 0)
        {
            query = query.Where(e => e.MarketId == marketId);
        }

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        var stations = await
            query
               .Select(e => new DTO.StationData
               {
                   Id = e.Id,
                   SystemAddress = e.SystemAddress,
                   BodyName = e.BodyName,
                   FirstSeen = e.FirstSeen,
                   IsRejected = e.IsRejected,
                   LastSeen = e.LastSeen,
                   Latitude = e.Latitude,
                   Longitude = e.Longitude,
                   MarketId = e.MarketId,
                   StationName = e.StationName,
                   StationType = e.StationType,
                   SystemName = e.SystemName,
                   ValidFrom = e.ValidFrom,
                   ValidTo = e.ValidTo
               })
               .OrderByDescending(e => e.LastSeen)
               .ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);

        var matches = brief
                    ? []
                    : await GetStationMatchEntriesAsync(stations.Keys, limitMatches, minDate, maxDate, canceltoken);

        var matchCounts = await GetStationMatchCountsAsync(stations.Keys, canceltoken);

        var entries = new Dictionary<int, StationData>();

        foreach (var (id, station) in stations)
        {
            entries[id] = station with
            {
                MatchCount = matchCounts.GetValueOrDefault(id),
                Matches = matches.GetValueOrDefault(id)
            };
        }

        return [.. entries.Values];
    }

    /// <summary>Lookup signals</summary>
    /// <param name="signalName"></param>
    /// <param name="systemName">Limit to events with given system name</param>
    /// <param name="systemAddress">Limit to events with given system address</param>
    /// <param name="brief">Set brief to only return signal information</param>
    /// <param name="limitMatches">Limit number of matches returned</param>
    /// <param name="minDate">Start of date range for matches</param>
    /// <param name="maxDate">End of date range for matches</param>
    /// <param name="canceltoken">Cancellation token</param>
    /// <returns>List of signals</returns>
    public async Task<List<SignalData>> GetSignalsAsync(
            string signalName,
            string? systemName,
            long? systemAddress,
            bool brief,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        if (string.IsNullOrWhiteSpace(signalName))
        {
            return [];
        }

        signalName = signalName.Trim();

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        var signalIds = await
            ctx.Set<Models.SignalInfo>()
               .Where(e => e.SignalName == signalName)
               .Select(e => e.Id)
               .ToListAsync(canceltoken);

        var signals = await
            ctx.Set<Models.SignalInfo>()
               .Where(e => signalIds.Contains(e.Id))
               .Select(e => new SignalData
               {
                   Id = e.Id,
                   SignalName = e.SignalName,
                   SignalType = e.SignalType,
                   IsStation = e.IsStation,
                   FirstSeen = e.FirstSeen,
                   LastSeen = e.LastSeen,
                   ValidFrom = e.ValidFrom,
                   ValidTo = e.ValidTo,
               })
               .OrderByDescending(e => e.LastSeen)
               .ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken);

        var signalSystems = new Dictionary<int, List<SignalSystem>>();
        var signalSets = new Dictionary<int, List<int>>();

        var query = ctx.Set<Models.SignalInfoSetItem>().AsQueryable();

        if ((systemName != null || systemAddress != null) && await GetSystemIdsAsync(systemName, systemAddress, canceltoken) is [..] systemIds)
        {
            query = query.Where(e => e.SystemId != null && systemIds.Contains(e.SystemId!.Value));
        }

        if (minDate != null)
        {
            query = query.Where(e => e.LastSeen >= minDate);
        }

        if (maxDate != null)
        {
            query = query.Where(e => e.FirstSeen <= maxDate);
        }

        foreach (int signalId in signalIds)
        {
            var systemSpans = await
                query
                    .Where(e => e.SignalInfoId == signalId && e.SystemId != null)
                    .GroupBy(e => e.SystemId!.Value)
                    .Select(e => new { SystemId = e.Key, FirstSeen = e.Min(v => v.FirstSeen), LastSeen = e.Max(v => v.LastSeen) })
                    .ToDictionaryAsync(e => e.SystemId, e => (e.FirstSeen, e.LastSeen), cancellationToken: canceltoken);

            var systems = await GetSystemsAsync<SignalSystem>(systemSpans.Keys, false, canceltoken);

            signalSystems[signalId] = [..
                systems
                    .Values
                    .Select(e => new SignalSystem
                    {
                        Id = e.Id,
                        Name = e.Name,
                        SystemAddress = e.SystemAddress,
                        FirstSeen = systemSpans.GetValueOrDefault(e.Id).FirstSeen,
                        LastSeen = systemSpans.GetValueOrDefault(e.Id).LastSeen
                    })
            ];

            signalSets[signalId] = await
                query
                    .OrderByDescending(e => e.LastSeen)
                    .Take(limitMatches ?? 1000)
                    .Select(e => e.SignalInfoSetId)
                    .ToListAsync(canceltoken);
        }

        var matches = brief
                    ? []
                    : await GetSignalMatchEntriesAsync(signalSets, limitMatches, minDate, maxDate, canceltoken);

        var matchCounts = await GetSignalMatchCountsAsync(signalSets, canceltoken);

        var entries = new Dictionary<int, SignalData>();

        foreach (var (id, signal) in signals)
        {
            entries[id] = signal with
            {
                SystemCount = signalSystems.GetValueOrDefault(id)?.Count,
                Systems = brief ? [] : signalSystems.GetValueOrDefault(id),
                MatchCount = (signalSets.GetValueOrDefault(id)?.Count == (limitMatches ?? 1000)) ? null : matchCounts.GetValueOrDefault(id),
                Matches = matches.GetValueOrDefault(id)
            };
        }

        return [.. entries.Values];
    }

    /// <summary>Extract EDDN event</summary>
    /// <remarks>
    /// Extract line from indexed EDDN capture
    /// </remarks>
    /// <param name="filename">EDDN capture filename without path</param>
    /// <param name="lineno">1-based Line number</param>
    /// <param name="canceltoken">Cancellation token</param>
    /// <returns></returns>
    public async Task<string?> ExtractLineAsync(string filename, int lineno, CancellationToken canceltoken)
    {
        if (_settings.IndexedDir == null
            || lineno <= 0
            || string.IsNullOrWhiteSpace(filename)
            || filename.ContainsAny(_fileSystem.Path.GetInvalidFileNameChars()))
        {
            return null;
        }

        filename = filename.Trim();

        lineno -= 1;

        int chunkNo = lineno / 1024;
        int itemNo = lineno % 1024;

        Models.FileInfo? file;

        await using (var ctx = await _contextFactory.CreateDbContextAsync(canceltoken))
        {
            file = await ctx.Set<Models.FileInfo>().FirstOrDefaultAsync(e => e.FileName == filename, cancellationToken: canceltoken);

            if (file == null || string.IsNullOrWhiteSpace(file.FileName) || file.FileName.ContainsAny(_fileSystem.Path.GetInvalidFileNameChars()))
            {
                return null;
            }
        }

        lock (_lineCacheLock)
        {
            if (_lineCache.TryGetValue(file.FileName, out var fileEnts)
                && fileEnts.Entries.TryGetValue(chunkNo, out var ents)
                && itemNo < ents.Value.Lines.Count)
            {
                if (ents.Previous != null)
                {
                    _lineCacheLRU.Remove(ents);
                    _lineCacheLRU.AddFirst(ents);
                }

                ents.ValueRef.LastUsed = DateTime.UtcNow;
                return ents.Value.Lines[itemNo];
            }
        }

        string indexFilename = _fileSystem.Path.Join(_settings.IndexedDir, $"{file.Date:yyyy-MM}", file.FileName);

        if (!_fileSystem.File.Exists(indexFilename) || !_fileSystem.File.Exists(indexFilename + ".index"))
        {
            return null;
        }

        var info = _fileSystem.FileInfo.New(indexFilename);
        var dataLastMod = info.LastWriteTimeUtc;
        long dataSize = info.Length;
        Span<byte> ixStartEndPos = stackalloc byte[16];

        for (int retries = 3; retries > 0; retries--)
        {
            using var indexStream = _fileSystem.File.Open(indexFilename + ".index", FileMode.Open, FileAccess.Read, FileShare.Read);

            var ixLastMod = indexStream is IFileSystemExtensibility indexFsExt
                         && indexFsExt.TryGetWrappedInstance<FileStream>(out var indexInnerStream)
                          ? _fileSystem.File.GetLastWriteTimeUtc(indexInnerStream.SafeFileHandle)
                          : _fileSystem.File.GetLastWriteTimeUtc(indexStream.Name);

            long ixSize = indexStream.Length;

            if (chunkNo >= (indexStream.Length / 8) - 1)
            {
                return null;
            }

            indexStream.Seek((long)chunkNo * 8, SeekOrigin.Begin);
            long startPos = 0;
            long endPos = 0;
            indexStream.ReadExactly(ixStartEndPos);
            startPos = BinaryPrimitives.ReadInt64LittleEndian(ixStartEndPos);
            endPos = BinaryPrimitives.ReadInt64LittleEndian(ixStartEndPos[8..]);

            if (endPos < startPos || endPos - startPos > 1048576)
            {
                return null;
            }

            using var dataStream = _fileSystem.File.Open(indexFilename, FileMode.Open, FileAccess.Read, FileShare.Read);

            long newSize = dataStream.Length;
            var newLastMod = dataStream is IFileSystemExtensibility dataFsExt
                          && dataFsExt.TryGetWrappedInstance<FileStream>(out var dataInnerStream)
                           ? _fileSystem.File.GetLastWriteTimeUtc(dataInnerStream.SafeFileHandle)
                           : _fileSystem.File.GetLastWriteTimeUtc(dataStream.Name);

            var ixInfo = _fileSystem.FileInfo.New(indexFilename + ".index");

            if (newSize != dataSize
                || newLastMod != dataLastMod
                || ixInfo.LastWriteTimeUtc != ixLastMod
                || ixInfo.Length != ixSize)
            {
                if (retries == 0)
                {
                    return null;
                }

                dataSize = newSize;
                dataLastMod = newLastMod;

                continue;
            }

            byte[] databuf = ArrayPool<byte>.Shared.Rent((int)(endPos - startPos));
            using var bzmemstream = new MemoryStream();

            try
            {
                dataStream.Seek(startPos, SeekOrigin.Begin);
                await dataStream.ReadExactlyAsync(databuf, 0, (int)(endPos - startPos), cancellationToken: canceltoken);
                bzmemstream.Write(databuf.AsSpan(0, (int)(endPos - startPos)));
                bzmemstream.Seek(0, SeekOrigin.Begin);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(databuf);
            }

            lock (_lineCacheLock)
            {
                if (!_lineCache.TryGetValue(file.FileName, out var fileEnts))
                {
                    _lineCache[file.FileName] = fileEnts = (dataLastMod, dataSize, []);
                }
                else if (fileEnts.LastMod != dataLastMod || fileEnts.Length != dataSize)
                {
                    foreach (var ent in fileEnts.Entries.Values)
                    {
                        _lineCacheLRU.Remove(ent);
                    }

                    _lineCache[file.FileName] = fileEnts = (dataLastMod, dataSize, []);
                }
                else if (fileEnts.Entries.TryGetValue(chunkNo, out var chunkEnts)
                         && itemNo < chunkEnts.Value.Lines.Count)
                {
                    chunkEnts.ValueRef.LastUsed = DateTime.UtcNow;
                    return chunkEnts.Value.Lines[itemNo];
                }

                using var memstream = new MemoryStream();
                using (var bzstream = new BZip2InputStream(bzmemstream))
                {
                    byte[] block = ArrayPool<byte>.Shared.Rent(65536);

                    try
                    {
                        while (bzstream.Read(block, 0, block.Length) is int len && len > 0)
                        {
                            memstream.Write(block, 0, len);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(block);
                    }
                }

                memstream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(memstream);

                var lines = new List<string>();

                while (reader.ReadLine() is string line)
                {
                    lines.Add(line);
                }

                fileEnts.Entries[chunkNo] = _lineCacheLRU.AddFirst((file.FileName, chunkNo, lines, DateTime.UtcNow));

                while (_lineCacheLRU.Last is { } last
                       && (_lineCacheLRU.Count > _maxCacheSize
                           || last.Value.LastUsed < DateTime.UtcNow - _maxCacheAge))
                {
                    _lineCacheLRU.Remove(last);

                    if (_lineCache.TryGetValue(last.Value.Filename, out var lastEnts))
                    {
                        lastEnts.Entries.Remove(last.Value.ChunkNo);
                    }

                    if (lastEnts.Entries.Count == 0)
                    {
                        _lineCache.Remove(last.Value.Filename);
                    }
                }

                return itemNo < lines.Count ? lines[itemNo] : null;
            }
        }

        return null;
    }

    /// <summary>Get systems in a sector</summary>
    /// <param name="sectorName">Name of the sector</param>
    /// <param name="nameOnly">Match name instead of SystemAddress</param>
    /// <param name="includeRejected">Set includeRejected to include items marked as rejected</param>
    /// <param name="canceltoken">Cancellation token</param>
    /// <param name="boxelName">Boxel suffix without N2 (sequence number)</param>
    /// <returns>List of systems</returns>
    public async IAsyncEnumerable<SectorSystem> GetSectorSystemsAsync(
            string sectorName,
            bool nameOnly,
            bool includeRejected,
            [EnumeratorCancellation] CancellationToken canceltoken,
            string? boxelName = null
        )
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        if (await ctx.Set<Models.Sector>().FirstOrDefaultAsync(e => e.Name == sectorName, canceltoken) is not { } sector)
        {
            yield break;
        }

        if (sector.SectorAddress == null && !nameOnly)
        {
            yield break;
        }

        long range = (1L << 40) - 1;
        long boxelid = 0;

        if (boxelName != null)
        {
            if (!boxelName.EndsWith('-'))
            {
                boxelName += "-";
            }

            if (!SystemHelpers.TrySplitProcgenName(sectorName + " " + boxelName + "0", out string? secname, out int mid, out int n2, out int masscode, false)
                || n2 != 0
                || mid < 0
                || mid >= 0x200000
                || masscode < 0
                || masscode >= 8
                || !string.Equals(secname, sectorName))
            {
                yield break;
            }

            boxelid = ((long)mid << 16) | ((long)masscode << 37);
            range = (1 << 16) - 1;
        }

        var sectorsById =
            ctx.Set<Models.Sector>()
               .Select(e => new { e.Id, e.Name })
               .ToDictionary(e => e.Id, e => e.Name);

        var sectorsByAddr =
            ctx.Set<Models.Sector>()
               .Where(e => e.SectorAddress != null)
               .Select(e => new { Id = e.SectorAddress!.Value, e.Name })
               .ToDictionary(e => e.Id, e => e.Name);

        var systemNames =
            ctx.Set<Models.SystemName>()
               .ToDictionary(e => (long)e.Id, e => e.Name);

        IQueryable<Models.SystemInfo> query = ctx.Set<Models.SystemInfo>();

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        if (nameOnly)
        {
            long minId = ((long)sector.Id << 40) + (1L << 60) + boxelid;
            long maxId = minId + range;

            await foreach (var system in query.Where(e => e.SystemNameId >= minId && e.SystemNameId <= maxId).AsAsyncEnumerable())
            {
                if (FillSystem<SectorSystem>(system, systemNames, sectorsById, sectorsByAddr) is { } entry)
                {
                    yield return entry;
                }
            }

            if (sector.SectorAddress is int sectorAddress)
            {
                long minAddr = ((long)sectorAddress << 40) + boxelid;
                long maxAddr = minAddr + range;

                await foreach (var system in query.Where(e => e.ModSystemAddress >= minAddr && e.ModSystemAddress <= maxAddr && e.ModSystemAddress != e.SystemNameId).AsAsyncEnumerable())
                {
                    if (FillSystem<SectorSystem>(system, systemNames, sectorsById, sectorsByAddr) is { } entry)
                    {
                        yield return entry;
                    }
                }
            }
        }
        else
        {
            if (sector.SectorAddress is int sectorAddress)
            {
                long minId = ((long)sectorAddress << 40) + boxelid;
                long maxId = minId + range;

                await foreach (var system in query.Where(e => e.ModSystemAddress >= minId && e.ModSystemAddress <= maxId).AsAsyncEnumerable())
                {
                    if (FillSystem<SectorSystem>(system, systemNames, sectorsById, sectorsByAddr) is { } entry)
                    {
                        yield return entry;
                    }
                }
            }
            else
            {
                yield break;
            }
        }
    }

    /// <summary>Get the list of known sectors</summary>
    /// <param name="includeSphereSectors">Include sphere sectors (AKA hand-authored sectors)</param>
    /// <param name="canceltoken">Cancellation token</param>
    /// <returns>List of sector names</returns>
    public async Task<List<string>> GetSectorsAsync(bool includeSphereSectors, CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.Sector> query = ctx.Set<Models.Sector>();

        if (!includeSphereSectors)
        {
            query = query.Where(e => e.SectorAddress != null);
        }

        return await query.Select(e => e.Name).Order().ToListAsync(canceltoken);
    }

    /// <summary>Get a list of systems in the gaps between known systems in a sector</summary>
    /// <param name="sectorName">Sector name</param>
    /// <param name="canceltoken">Cancellation Token</param>
    /// <param name="boxelName">Boxel suffix without N2 (sequence number)</param>
    /// <returns>List of gap systems</returns>
    public async IAsyncEnumerable<SystemGapData> EnumerateGapSystemsAsync(
            string sectorName,
            [EnumeratorCancellation] CancellationToken canceltoken,
            string? boxelName = null
        )
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        if (ctx.Set<Models.Sector>().FirstOrDefault(e => e.Name == sectorName) is not { } sector
            || sector.SectorAddress is not int sectorAddress)
        {
            yield break;
        }

        long minAddr = (long)sectorAddress << 40;
        long maxAddr = minAddr + (1L << 40) - 1;

        if (boxelName is [.., char boxelend])
        {
            if (boxelend is not ('-' or (>= 'a' and <= 'h')))
            {
                boxelName += "-";
            }

            if (!SystemHelpers.TrySplitProcgenName(sectorName + " " + boxelName + "0", out string? secname, out int mid, out int n2, out int masscode, false)
                || n2 != 0
                || mid is < 0 or >= 0x200000
                || masscode is < 0 or >= 8
                || !string.Equals(secname, sectorName))
            {
                yield break;
            }

            long boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);
            minAddr += boxelid;
            maxAddr = minAddr + (1 << 16) - 1;
        }

        var query =
            ctx.Set<Models.SystemInfo>()
               .Where(e => e.ModSystemAddress >= minAddr && e.ModSystemAddress <= maxAddr)
               .Select(e => new
               {
                   e.ModSystemAddress,
                   e.FirstSeen,
                   e.LastSeen,
                   e.IsRejected,
                   HasCoords = e.X != null && e.Y != null && e.Z != null
               })
               .OrderBy(e => e.ModSystemAddress)
               .AsAsyncEnumerable();

        long prevSystemAddress = 0;
        long boxelModSystemAddress = 0;
        string? prevPrefix = null;
        int prevSeqnum = 0;
        DateTime? prevFirstSeen = null;
        DateTime? prevLastSeen = null;

        await foreach (var entry in query)
        {
            if (entry.ModSystemAddress is not long modsysaddr
                || SystemHelpers.GetPGSuffix(modsysaddr, false) is not string pgsuffix)
            {
                continue;
            }

            string prefix = sector.Name + pgsuffix;
            int seqnum = (int)(modsysaddr & 0xFFFF);

            if (prefix != prevPrefix || seqnum != prevSeqnum)
            {
                if (prevPrefix != null)
                {
                    yield return new SystemGapData
                    {
                        SystemAddress = prevSystemAddress,
                        NamePrefix = prevPrefix,
                        SequenceNumber = prevSeqnum,
                        FirstSeen = prevFirstSeen,
                        LastSeen = prevLastSeen
                    };
                }

                prevSeqnum = prefix == prevPrefix ? prevSeqnum + 1 : 0;
                prevPrefix = prefix;
                prevFirstSeen = null;
                prevLastSeen = null;
                boxelModSystemAddress = modsysaddr & ~0xFFFF;
            }

            while (prevSeqnum < seqnum)
            {
                long sysaddr = SystemHelpers.ModSystemAddressToSystemAddress(boxelModSystemAddress + prevSeqnum) ?? throw new UnreachableException();

                yield return new SystemGapData
                {
                    SystemAddress = sysaddr,
                    NamePrefix = prefix,
                    SequenceNumber = prevSeqnum
                };

                prevSeqnum++;
            }

            prevSystemAddress = SystemHelpers.ModSystemAddressToSystemAddress(boxelModSystemAddress + prevSeqnum) ?? throw new UnreachableException();

            if (entry.IsRejected != true && entry.HasCoords)
            {
                if (prevFirstSeen == null || prevFirstSeen > entry.FirstSeen)
                {
                    prevFirstSeen = entry.FirstSeen;
                }

                if (prevLastSeen == null || prevLastSeen < entry.LastSeen)
                {
                    prevLastSeen = entry.LastSeen;
                }
            }
        }

        if (prevPrefix != null)
        {
            yield return new SystemGapData
            {
                SystemAddress = prevSystemAddress,
                NamePrefix = prevPrefix,
                SequenceNumber = prevSeqnum,
                FirstSeen = prevFirstSeen,
                LastSeen = prevLastSeen
            };
        }
    }

    /// <summary>Get table info</summary>
    /// <param name="canceltoken">Cancellation Token</param>
    /// <returns>Table info</returns>
    public async Task<Dictionary<string, TableInfo>> GetTableInfoAsync(CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        if (ctx.Database.IsMySql())
        {
            var results = await ctx.Database.SqlQueryRaw<TableInfo>("""
                SELECT
                    TABLE_NAME AS TableName,
                    TABLE_ROWS AS RowCount,
                    DATA_LENGTH AS DataSize,
                    INDEX_LENGTH AS IndexSize,
                    DATA_LENGTH + INDEX_LENGTH AS TotalSize,
                    IF(TABLE_ROWS = 0, 0, (DATA_LENGTH + INDEX_LENGTH) * 1.0 / TABLE_ROWS) AS BytesPerRow
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME NOT IN ('__EFMigrationsHistory')
            """).ToListAsync(canceltoken);

            return results.ToDictionary(e => e.TableName, StringComparer.OrdinalIgnoreCase);
        }
        else if (ctx.Database.IsSqlServer())
        {
            return await ctx.Database.SqlQueryRaw<TableInfo>("""
                SELECT
                    t.TableName,
                    t.[RowCount],
                    t.DataSize,
                    t.IndexSize,
                    t.TotalSize,
                    IIF(t.[RowCount] = 0, 0, t.TotalSize * 1.0 / t.[RowCount]) AS BytesPerRow
                FROM (
                SELECT
                    t.name AS TableName,
                    SUM(CASE WHEN i.index_id IN (0, 1) THEN p.rows ELSE 0 END) AS [RowCount],
                    SUM(CASE WHEN i.index_id IN (0, 1) THEN au.total_pages ELSE 0 END) * 8192 AS [DataSize],
                    SUM(CASE WHEN i.index_id > 1 THEN au.total_pages ELSE 0 END) * 8192 AS [IndexSize],
                    SUM(au.total_pages) * 8192 AS [TotalSize]
                FROM sys.tables t
                INNER JOIN sys.schemas s
                    ON s.schema_id = t.schema_id
                INNER JOIN sys.indexes i
                    ON i.object_id = t.object_id
                INNER JOIN sys.partitions p
                    ON p.object_id = i.object_id
                   AND p.index_id = i.index_id
                INNER JOIN sys.allocation_units au
                    ON au.container_id =
                       CASE au.type
                           WHEN 2 THEN p.partition_id
                           ELSE p.hobt_id
                       END
                WHERE t.is_ms_shipped = 0
                  AND s.name = (SELECT default_schema_name FROM sys.database_principals WHERE name = USER_NAME())
                  AND t.name NOT IN ('__EFMigrationsHistory')
                GROUP BY
                    s.name,
                    t.name
                ) t
            """).ToDictionaryAsync(e => e.TableName, StringComparer.OrdinalIgnoreCase, canceltoken);
        }
        else if (ctx.Database.IsNpgsql())
        {
            return await ctx.Database.SqlQueryRaw<TableInfo>("""
                SELECT
                    c.relname AS "TableName",
                    COALESCE(s.n_live_tup, c.reltuples)::bigint AS "RowCount",
                    pg_relation_size(c.oid)::bigint AS "DataSize",
                    pg_indexes_size(c.oid)::bigint AS "IndexSize",
                    pg_total_relation_size(c.oid)::bigint AS "TotalSize",
                    CASE
                        WHEN COALESCE(s.n_live_tup, c.reltuples) > 0
                            THEN pg_relation_size(c.oid)::double precision / COALESCE(s.n_live_tup, c.reltuples)
                        ELSE 0
                    END AS "BytesPerRow"
                FROM pg_class AS c
                JOIN pg_namespace AS n
                    ON n.oid = c.relnamespace
                LEFT JOIN pg_stat_user_tables AS s
                    ON s.relid = c.oid
                WHERE c.relkind = 'r'
                  AND n.nspname = current_schema()
                  AND c.relname NOT IN ('__EFMigrationsHistory')
            """).ToDictionaryAsync(e => e.TableName, StringComparer.OrdinalIgnoreCase, canceltoken);
        }
        else
        {
            return [];
        }
    }

    /// <summary>Get directory usage stats</summary>
    /// <param name="canceltoken">Cancellation Token</param>
    /// <returns>Directory usage stats</returns>
    public async Task<Dictionary<string, DumpDirectoryUsage>> GetDirectoryUsagesAsync(CancellationToken canceltoken)
    {
        var dirusages = new Dictionary<string, DumpDirectoryUsage>();

        foreach (var (name, path) in _settings.DumpDirs)
        {
            var (size, filecount) = await
                _fileSystem
                    .Directory
                    .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Select(_fileSystem.FileInfo.New)
                    .ToAsyncEnumerable()
                    .AggregateAsync((size: 0L, filecount: 0), (a, e) => (a.size + e.Length, a.filecount + 1), canceltoken);

            dirusages[name] = new DumpDirectoryUsage
            {
                DirectoryName = name,
                DataSize = size,
                FileCount = filecount
            };
        }

        return dirusages;
    }

    /// <summary>Get storage statistics</summary>
    /// <param name="canceltoken">Cancellation Token</param>
    /// <returns>Storage stats</returns>
    public async Task<StorageStats> GetStorageStatsAsync(CancellationToken canceltoken)
    {
        return new StorageStats
        {
            Tables = await GetTableInfoAsync(canceltoken),
            DumpUsages = await GetDirectoryUsagesAsync(canceltoken)
        };
    }
}
