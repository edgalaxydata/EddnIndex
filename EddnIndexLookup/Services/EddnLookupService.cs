using EddnIndexLookup.DTO;
using EddnIndexLookup.Options;
using EddnIndexUpdate;
using Ionic.BZip2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Models = EddnIndexUpdate.Models;
using Sectors = EddnIndexUpdate.Sectors;

namespace EddnIndexLookup.Services;

/// <summary>
/// Backend service for EDDN lookup API
/// </summary>
/// <param name="contextFactory"></param>
/// <param name="logger"></param>
/// <param name="options"></param>
public class EddnLookupService(
        IDbContextFactory<Models.EDDNContext> contextFactory,
        ILogger<FileProcessor> logger,
        IOptions<EddnLookupServiceSettings> options
    )
{
    private readonly IDbContextFactory<Models.EDDNContext> ContextFactory = contextFactory;
    private readonly ILogger Logger = logger;
    private readonly EddnLookupServiceSettings Settings = options.Value;
    private readonly Dictionary<string, (DateTime LastMod, long Length, Dictionary<int, LinkedListNode<(string Filename, int ChunkNo, List<string> Lines, DateTime LastUsed)>> Entries)> LineCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<(string Filename, int ChunkNo, List<string> Lines, DateTime LastUsed)> LineCacheLRU = [];
    private readonly Lock LineCacheLock = new();

    private readonly TimeSpan MaxCacheAge = TimeSpan.FromHours(1);

    private async IAsyncEnumerable<long> GetSystemNameIdsAsync(string? name, [EnumeratorCancellation] CancellationToken canceltoken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            yield break;
        }

        name = name.Trim();

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        foreach (var entry in await ctx.Set<Models.SystemName>().Where(e => e.Name == name).ToListAsync(canceltoken))
        {
            yield return -entry.Id;
        }

        if (Models.SystemInfo.TrySplitProcgenName(name, out var sectorName, out var mid, out var n2, out var masscode, true)
            && n2 >= 0
            && n2 < 65536
            && mid >= 0
            && mid < 0x200000
            && masscode >= 0
            && masscode < 8)
        {
            var boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);

            foreach (var sector in ctx.Set<Models.Sector>().Where(e => e.Name == sectorName))
            {
                if (sector.SectorAddress is int sectoraddr && sectoraddr >= 0 && sectoraddr < 0x100000)
                {
                    yield return (long)sectoraddr << 40 | boxelid;
                }

                yield return ((long)sector.Id + 0x100000) << 40 | boxelid;
            }
        }
    }

    private async Task<Dictionary<string, List<long>>> GetSystemNameIdsAsync(ICollection<string> names, CancellationToken canceltoken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

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

        foreach (var sysname in names)
        {
            var ids = new List<long>();

            if (Models.SystemInfo.TrySplitProcgenName(sysname, out string? sectorname, out int mid, out int n2, out int masscode, true)
                && n2 >= 0
                && n2 < 65536
                && mid >= 0
                && mid < 0x200000
                && masscode >= 0
                && masscode < 8
                && sectors.TryGetValue(sectorname, out var sectorEnts))
            {
                var boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);

                foreach (var sector in sectorEnts)
                {
                    if (sector.SectorAddress is int sectoraddr && sectoraddr >= 0 && sectoraddr < 0x100000)
                    {
                        ids.Add((long)sectoraddr << 40 | boxelid);
                    }

                    ids.Add(((long)sector.Id + 0x100000) << 40 | boxelid);
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

    private async IAsyncEnumerable<(long? SystemNameId, int BodyNameId)> GetBodyNameIdsAsync(string? name, [EnumeratorCancellation] CancellationToken canceltoken)
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

        for (var spacePos = name.LastIndexOf(' '); spacePos > 0; spacePos = name.LastIndexOf(' ', spacePos - 1))
        {
            nameEnts[name[spacePos..]] = name[..spacePos];
        }

        var sysNamesToIds = await GetSystemNameIdsAsync(nameEnts.Values, canceltoken);

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

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
            if (nameEnts.TryGetValue(desigName, out var sysname) && sysNamesToIds.TryGetValue(sysname, out var sysNameIds))
            {
                foreach (var desigEnt in desigEnts)
                {
                    foreach (var sysNameId in sysNameIds)
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

    private async Task<Dictionary<int, SystemData>?> GetSystemsAsync(string? systemName, long? systemAddress, bool includeRejected, CancellationToken canceltoken)
    {
        if (string.IsNullOrWhiteSpace(systemName) && (systemAddress == null || systemAddress <= 0))
        {
            return null;
        }

        List<long> sysNameIds = await GetSystemNameIdsAsync(systemName, canceltoken).ToListAsync(canceltoken);

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.SystemInfo> query = ctx.Set<Models.SystemInfo>();

        if (sysNameIds.Count != 0)
        {
            query = query.Where(e => e.SystemNameId != null && sysNameIds.Contains(e.SystemNameId.Value));
        }

        if (Models.SystemInfo.SystemAddressToModSystemAddress(systemAddress) is long modsysaddr)
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

        return await FillSystemsAsync(ctx, systems, canceltoken);
    }

    private static async Task<Dictionary<int, SystemData>> FillSystemsAsync(Models.EDDNContext ctx, Dictionary<int, Models.SystemInfo> systems, CancellationToken canceltoken)
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
               .ToDictionaryAsync(e => (long)e.Id + 0x100000, e => e.Name, cancellationToken: canceltoken);

        var sectorsByAddr = await
            ctx.Set<Models.Sector>()
               .Where(e => e.SectorAddress != null && sectorIds.Contains(e.SectorAddress.Value))
               .ToDictionaryAsync(e => e.SectorAddress!.Value, e => e.Name, cancellationToken: canceltoken);

        var systemDatas = new Dictionary<int, SystemData>();

        foreach (var (systemId, system) in systems)
        {
            var name = system switch
            {
                { SystemNameId: long sysNameId }
                    when systemNames.TryGetValue(-sysNameId, out var sysname)
                    => sysname,
                { SectorId: int sectorId, PGSuffix: string pgSuffix }
                    when sectorsById.TryGetValue(sectorId, out var sectorName)
                    => sectorName + pgSuffix,
                { SectorAddress: int sectorAddr, PGSuffix: string pgSuffix }
                    => (sectorsByAddr.GetValueOrDefault(sectorAddr) ?? Sectors.PGSectors.GetSectorName(sectorAddr)) + pgSuffix,
                _ => null
            };

            if (name != null)
            {
                systemDatas[systemId] = new DTO.SystemData
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
        }

        return systemDatas;
    }

    private static async Task<Dictionary<long, BodyData>> FillBodiesAsync(Models.EDDNContext ctx, Dictionary<long, Models.BodyInfo> bodies, CancellationToken canceltoken)
    {
        var bodyNameIds =
            bodies
                .Values
                .Select(e => e.BodyNameId)
                .OfType<int>()
                .Union(bodies.Values.Select(e => e.BodyDesignationId).OfType<int>())
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
               .Where(e => bodyNameIds.Contains(-e.Id))
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

        var bodiesData = new Dictionary<long, BodyData>();

        foreach (var (id, body) in bodies)
        {
            var sysname = body switch
            {
                { SystemNameId: long sysNameId }
                    when systemNames.TryGetValue(-sysNameId, out var sn)
                    => sn,
                { SysName_SectorId: int sectorId, SysName_PGSuffix: string pgSuffix }
                    when sectorsById.TryGetValue(sectorId, out var sectorName)
                    => sectorName + pgSuffix,
                { SysName_SectorAddress: int sectorAddr, SysName_PGSuffix: string pgSuffix }
                    => (sectorsByAddr.GetValueOrDefault(sectorAddr) ?? Sectors.PGSectors.GetSectorName(sectorAddr)) + pgSuffix,
                _ => null
            };

            var desigSysName = body.System switch
            {
                { SystemNameId: long sysNameId }
                    when systemNames.TryGetValue(-sysNameId, out var sn)
                    => sn,
                { SectorId: int sectorId, PGSuffix: string pgSuffix }
                    when sectorsById.TryGetValue(sectorId, out var sectorName)
                    => sectorName + pgSuffix,
                { SectorAddress: int sectorAddr, PGSuffix: string pgSuffix }
                    => (sectorsByAddr.GetValueOrDefault(sectorAddr) ?? Sectors.PGSectors.GetSectorName(sectorAddr)) + pgSuffix,
                _ => null
            };

            if (body.BodyNameId is int bodyNameId)
            {
                var name = (sysname, bodyNameId) switch
                {
                    (_, > 0) when bodyNames.TryGetValue(bodyNameId, out var bn) => bn.Name,
                    (not null, < 0) when bodyDesigsById.TryGetValue(-bodyNameId, out var bd) => sysname + bd.Designation,
                    (not null, > 0) when bodyDesigsByDesigId.TryGetValue(bodyNameId, out var bd) => sysname + bd.Designation,
                    _ => null
                };

                var desig = body.BodyDesignationId is not int bodyDesigId
                          ? null
                          : (desigSysName, bodyDesigId) switch
                          {
                              (_, > 0) when bodyNames.TryGetValue(bodyNameId, out var bn) => bn.Name,
                              (not null, < 0) when bodyDesigsById.TryGetValue(-bodyNameId, out var bd) => desigSysName + bd.Designation,
                              (not null, > 0) when bodyDesigsByDesigId.TryGetValue(bodyNameId, out var bd) => desigSysName + bd.Designation,
                              _ => null
                          };

                if (name != null)
                {
                    bodiesData[id] = new DTO.BodyData
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
                                : null
                    };
                }
            }
        }

        return bodiesData;
    }

    private async Task<Dictionary<long, BodyData>?> GetBodiesAsync(string? systemName, long? systemAddress, string? bodyName, int? bodyId, bool includeRejected, CancellationToken canceltoken)
    {
        var systems = await GetSystemsAsync(systemName, systemAddress, includeRejected, canceltoken);

        if ((systems == null || bodyId == null || bodyId < 0) && string.IsNullOrWhiteSpace(bodyName))
        {
            return null;
        }

        var sysAndBodyNameIds = await GetBodyNameIdsAsync(bodyName, canceltoken).ToListAsync(canceltoken);
        var sysNameIds = sysAndBodyNameIds.Select(e => e.SystemNameId).OfType<long>().ToList();
        var bodyNameIds = sysAndBodyNameIds.Where(e => e.SystemNameId == null).Select(e => e.BodyNameId).ToList();
        var bodyDesigIds = sysAndBodyNameIds.Where(e => e.SystemNameId != null).Select(e => e.BodyNameId).ToList();

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.BodyInfo> query =
            ctx.Set<Models.BodyInfo>()
               .Include(e => e.ParentSet)
               .Include(e => e.System);

        if (bodyName != null)
        {
            query = query.Where(e => bodyNameIds.Contains(e.BodyNameId!.Value)
                                  || (sysNameIds.Contains(e.SystemNameId!.Value) && bodyDesigIds.Contains(e.BodyNameId!.Value)));
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

        return await FillBodiesAsync(ctx, bodies, canceltoken);
    }

    private async Task<Dictionary<int, SystemData>> GetSystemsAsync(ICollection<int> systemIds, bool includeRejected, CancellationToken canceltoken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        var query =
            ctx.Set<Models.SystemInfo>()
               .Where(e => systemIds.Contains(e.Id));

        if (!includeRejected)
        {
            query = query.Where(e => e.IsRejected != true);
        }

        return await FillSystemsAsync(ctx, await query.ToDictionaryAsync(e => e.Id, cancellationToken: canceltoken), canceltoken);
    }

    private async Task<Dictionary<int, Dictionary<long, BodyData>>> GetSystemBodiesAsync(ICollection<int> systemIds, bool includeRejected, CancellationToken canceltoken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

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

        var bodies2 = await FillBodiesAsync(ctx, bodies, canceltoken);

        return
            bodies2
                .Values
                .GroupBy(e => e.SystemId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(e => e.Id));
    }

    private async Task<Dictionary<int, List<MatchEntry>>> GetSystemMatchEntriesAsync(
            ICollection<int> systemIds,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        var routeQuery =
            ctx.Set<Models.FileLineNavRoute>()
               .Where(e => systemIds.Contains(e.SystemId))
               .Join(
                    ctx.Set<Models.FileInfo>(),
                    o => o.FileId,
                    i => i.Id,
                    (o, i) => new { RouteEntry = o, File = i }
                )
               .Join(
                    ctx.Set<Models.FileLineInfo>()
                       .Include(e => e.Software)
                       .Include(e => e.SchemaEvent)
                       .Include(e => e.GameVersion),
                    o => new { o.RouteEntry.FileId, o.RouteEntry.LineNo },
                    i => new { i.FileId, i.LineNo },
                    (o, i) => new { o.File, Info = i, o.RouteEntry }
               )
               .Select(e => new DTO.MatchEntry
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
                   GatewayTimestamp = e.Info.GatewayTimestamp,
                   SystemId = e.RouteEntry.SystemId
               });

        var query =
            ctx.Set<Models.FileLineInfo>()
               .Where(e => systemIds.Contains(e.SystemId!.Value))
               .Include(e => e.Software)
               .Include(e => e.SchemaEvent)
               .Include(e => e.GameVersion)
               .Join(
                    ctx.Set<Models.FileInfo>(),
                    o => o.FileId,
                    i => i.Id,
                    (o, i) => new { Info = o, File = i }
                )
               .LeftJoin(
                    ctx.Set<Models.FileLineBody>(),
                    o => new { o.Info.FileId, o.Info.LineNo },
                    i => new { i.FileId, i.LineNo },
                    (o, i) => new { o.File, o.Info, Body = i }
               )
               .LeftJoin(
                    ctx.Set<Models.FileLineStation>()
                       .Include(e => e.Station),
                    o => new { o.Info.FileId, o.Info.LineNo },
                    i => new { i.FileId, i.LineNo },
                    (o, i) => new { o.File, o.Info, o.Body, Station = i }
               )
               .Select(e => new DTO.MatchEntry
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
                    BodyId = e.Body!.BodyId,
                    StationId = e.Station == null ? null : e.Station.StationId
               });

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.GatewayTimestamp >= minTS);
            routeQuery = routeQuery.Where(e => e.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.GatewayTimestamp <= maxTS);
            routeQuery = routeQuery.Where(e => e.GatewayTimestamp <= maxTS);
        }

        return await
            query
                .OrderByDescending(e => e.GatewayTimestamp)
                .Take(limitMatches ?? 5000)
                .AsAsyncEnumerable()
                .Concat(routeQuery.OrderByDescending(e => e.GatewayTimestamp).Take(limitMatches ?? 5000).AsAsyncEnumerable())
                .OrderByDescending(e => e.GatewayTimestamp)
                .Take(limitMatches ?? 5000)
                .Where(e => e.SystemId != null)
                .GroupBy(e => e.SystemId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken: canceltoken);
    }

    private async Task<Dictionary<long, List<MatchEntry>>> GetBodyMatchEntriesAsync(
            ICollection<long> bodyIds,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        var query =
            ctx.Set<Models.FileLineBody>()
               .Where(e => bodyIds.Contains(e.BodyId))
               .Join(
                    ctx.Set<Models.FileInfo>(),
                    o => o.FileId,
                    i => i.Id,
                    (o, i) => new { Body = o, File = i }
                )
               .Join(
                    ctx.Set<Models.FileLineInfo>()
                       .Include(e => e.Software)
                       .Include(e => e.SchemaEvent)
                       .Include(e => e.GameVersion),
                    o => new { o.Body.FileId, o.Body.LineNo },
                    i => new { i.FileId, i.LineNo },
                    (o, i) => new { o.File, Info = i, o.Body }
               )
               .LeftJoin(
                    ctx.Set<Models.FileLineStation>()
                       .Include(e => e.Station),
                    o => new { o.Body.FileId, o.Body.LineNo },
                    i => new { i.FileId, i.LineNo },
                    (o, i) => new { o.File, o.Info, o.Body, Station = i }
               )
               .Select(e => new DTO.MatchEntry
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
                    BodyId = e.Body.BodyId,
                    StationId = e.Station == null ? null : e.Station.StationId
               });

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.GatewayTimestamp <= maxTS);
        }

        return await
            query
                .OrderByDescending(e => e.GatewayTimestamp)
                .Take(limitMatches ?? 5000)
                .AsAsyncEnumerable()
                .Where(e => e.BodyId != null)
                .GroupBy(e => e.BodyId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken: canceltoken);
    }

    private async Task<Dictionary<int, List<MatchEntry>>> GetStationMatchEntriesAsync(
            ICollection<int> stationIds,
            int? limitMatches,
            DateTimeOffset? minDate,
            DateTimeOffset? maxDate,
            CancellationToken canceltoken
        )
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        var query =
            ctx.Set<Models.FileLineStation>()
               .Where(e => stationIds.Contains(e.StationId))

               .Join(
                    ctx.Set<Models.FileInfo>(),
                    o => o.FileId,
                    i => i.Id,
                    (o, i) => new { Station = o, File = i }
                )
               .Join(
                    ctx.Set<Models.FileLineInfo>()
                       .Include(e => e.Software)
                       .Include(e => e.SchemaEvent)
                       .Include(e => e.GameVersion),
                    o => new { o.Station.FileId, o.Station.LineNo },
                    i => new { i.FileId, i.LineNo },
                    (o, i) => new { o.File, Info = i, o.Station }
               )
               .LeftJoin(
                    ctx.Set<Models.FileLineBody>(),
                    o => new { o.Station.FileId, o.Station.LineNo },
                    i => new { i.FileId, i.LineNo },
                    (o, i) => new { o.File, o.Info, Body = i, o.Station }
               )
               .Select(e => new DTO.MatchEntry
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
                    StationId = e.Station.StationId
               });

        if (minDate?.ToUniversalTime().DateTime is DateTime minTS)
        {
            query = query.Where(e => e.GatewayTimestamp >= minTS);
        }

        if (maxDate?.ToUniversalTime().DateTime is DateTime maxTS)
        {
            query = query.Where(e => e.GatewayTimestamp <= maxTS);
        }

        var matches = await
            query
                .OrderByDescending(e => e.GatewayTimestamp)
                .Take(limitMatches ?? 5000)
                .AsAsyncEnumerable()
                .Where(e => e.StationId != null)
                .GroupBy(e => e.StationId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.ToList(), cancellationToken: canceltoken);

        var systemIds = matches.Values.SelectMany(e => e).Select(e => e.SystemId).OfType<int>().ToList();

        var systems = await GetSystemsAsync(systemIds, true, canceltoken);

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

    private async Task<Dictionary<int, int>> GetSystemMatchCountsAsync(ICollection<int> systemIds, CancellationToken canceltoken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        return await
            ctx.Set<Models.FileLineInfo>()
               .Where(e => e.SystemId != null && systemIds.Contains(e.SystemId.Value))
               .GroupBy(e => e.SystemId!.Value)
               .Select(g => new { SystemId = g.Key, Count = g.Count() })
               .ToDictionaryAsync(e => e.SystemId, e => e.Count, cancellationToken: canceltoken);
    }

    private async Task<Dictionary<long, int>> GetBodyMatchCountsAsync(ICollection<long> bodyIds, CancellationToken canceltoken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        return await
            ctx.Set<Models.FileLineBody>()
               .Where(e => bodyIds.Contains(e.BodyId))
               .GroupBy(e => e.BodyId)
               .Select(g => new { BodyId = g.Key, Count = g.Count() })
               .ToDictionaryAsync(e => e.BodyId, e => e.Count, cancellationToken: canceltoken);
    }

    private async Task<Dictionary<int, int>> GetStationMatchCountsAsync(ICollection<int> stationIds, CancellationToken canceltoken)
    {
        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        return await
            ctx.Set<Models.FileLineStation>()
               .Where(e => stationIds.Contains(e.StationId))
               .GroupBy(e => e.StationId)
               .Select(g => new { BodyId = g.Key, Count = g.Count() })
               .ToDictionaryAsync(e => e.BodyId, e => e.Count, cancellationToken: canceltoken);
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
        if (await GetSystemsAsync(systemName, systemAddress, includeRejected, canceltoken) is not { } systems)
        {
            return [];
        }

        var bodies = await GetSystemBodiesAsync(systems.Keys, includeRejected, canceltoken);

        var bodyIds =
            bodies
                .Values
                .SelectMany(e => e)
                .Select(e => e.Key)
                .Distinct()
                .ToList();

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

        var systemMatchCounts = await GetSystemMatchCountsAsync(systems.Keys, canceltoken);
        var bodyMatchCounts = await GetBodyMatchCountsAsync(bodyIds, canceltoken);

        var entries = new List<SystemData>();

        foreach (var (systemId, system) in systems)
        {
            var sysEntry = system with
            {
                MatchCount = systemMatchCounts.GetValueOrDefault(systemId),
                Matches = systemMatches.GetValueOrDefault(systemId),
                Bodies = []
            };

            entries.Add(sysEntry);

            foreach (var (bodyId, body) in bodies.GetValueOrDefault(systemId) ?? [])
            {
                sysEntry.Bodies.Add(new SystemBodyData
                {
                    Name = body.Name,
                    ArgOfPeriapsis = body.ArgOfPeriapsis,
                    SemiMajorAxis = body.SemiMajorAxis,
                    BodyId = body.BodyId,
                    SystemAddress = body.SystemAddress,
                    Designation = body.Designation,
                    FirstSeen = body.FirstSeen,
                    Id = body.Id,
                    Inclination = body.Inclination,
                    IsRejected = body.IsRejected,
                    LastSeen = body.LastSeen,
                    MatchCount = bodyMatchCounts.GetValueOrDefault(bodyId),
                    Matches = bodyMatches.GetValueOrDefault(bodyId),
                    Parents = body.Parents,
                    SystemId = body.SystemId,
                    ValidFrom = body.ValidFrom,
                    ValidTo = body.ValidTo
                });
            }
        }

        return entries;
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

        var systems = await GetSystemsAsync(systemIds, includeRejected, canceltoken);

        var bodyMatches = brief
                        ? []
                        : await GetBodyMatchEntriesAsync(bodies.Keys, limitMatches, minDate, maxDate, canceltoken);
        
        var bodyMatchCounts = await GetBodyMatchCountsAsync(bodies.Keys, canceltoken);

        var entries = new List<BodyData>();

        foreach (var (id, body) in bodies)
        {
            var bodyEnt = body with
            {
                MatchCount = bodyMatchCounts.GetValueOrDefault(id),
                Matches = bodyMatches.GetValueOrDefault(id),
                System = systems.TryGetValue(body.SystemId, out var system) ? new DTO.BodySystem
                {
                    Name = system.Name,
                    NameSystemAddress = system.NameSystemAddress,
                    SystemAddress = system.SystemAddress,
                    Coords = system.Coords,
                    PGName = system.PGName
                } : null
            };

            entries.Add(bodyEnt);
        }

        return entries;
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

        await using var ctx = await ContextFactory.CreateDbContextAsync(canceltoken);

        IQueryable<Models.StationInfo> query = ctx.Set<Models.StationInfo>();

        if (!string.IsNullOrWhiteSpace(stationName))
        {
            query = query.Where(e => e.StationName == stationName);
        }

        if (marketId != null && marketId > 0)
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

        return entries.Values.ToList();
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
        if (Settings.IndexedDir == null || lineno <= 0 || string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        filename = filename.Trim();

        lineno -= 1;

        var chunkNo = lineno / 1024;
        var itemNo = lineno % 1024;

        Models.FileInfo? file;

        await using (var ctx = await ContextFactory.CreateDbContextAsync(canceltoken))
        {
            file = await ctx.Set<Models.FileInfo>().FirstOrDefaultAsync(e => e.FileName == filename, cancellationToken: canceltoken);

            if (file == null)
            {
                return null;
            }
        }

        lock (LineCacheLock)
        {
            if (LineCache.TryGetValue(file.FileName, out var fileEnts)
                && fileEnts.Entries.TryGetValue(chunkNo, out var ents)
                && itemNo < ents.Value.Lines.Count)
            {
                if (ents.Previous != null)
                {
                    LineCacheLRU.Remove(ents);
                    LineCacheLRU.AddFirst(ents);
                }

                ents.ValueRef.LastUsed = DateTime.UtcNow;
                return ents.Value.Lines[itemNo];
            }
        }

        var indexFilename = Path.Combine(Settings.IndexedDir, $"{file.Date:yyyy-MM}", file.FileName);

        if (!File.Exists(indexFilename) || !File.Exists(file.FileName + ".index"))
        {
            return null;
        }

        var info = new FileInfo(indexFilename);
        var dataLastMod = info.LastWriteTimeUtc;
        var dataSize = info.Length;
        Span<byte> ixStartEndPos = stackalloc byte[16];

        for (int retries = 3; ; retries--)
        {
            using var indexStream = File.Open(indexFilename + ".index", FileMode.Open, FileAccess.Read, FileShare.Read);

            var ixLastMod = File.GetLastWriteTimeUtc(indexStream.SafeFileHandle);
            var ixSize = indexStream.Length;

            if (chunkNo >= indexStream.Length / 8 - 1)
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

            using var dataStream = File.Open(indexFilename, FileMode.Open, FileAccess.Read, FileShare.Read);

            var newSize = dataStream.Length;
            var newLastMod = File.GetLastWriteTimeUtc(dataStream.SafeFileHandle);

            var ixInfo = new FileInfo(indexFilename + ".index");

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

            var databuf = ArrayPool<byte>.Shared.Rent((int)(endPos - startPos));
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

            lock (LineCacheLock)
            {
                if (!LineCache.TryGetValue(file.FileName, out var fileEnts))
                {
                    LineCache[file.FileName] = fileEnts = (dataLastMod, dataSize, []);
                }
                else if (fileEnts.LastMod != dataLastMod || fileEnts.Length != dataSize)
                {
                    foreach (var ent in fileEnts.Entries.Values)
                    {
                        LineCacheLRU.Remove(ent);
                    }

                    LineCache[file.FileName] = fileEnts = (dataLastMod, dataSize, []);
                }
                else if (fileEnts.Entries.TryGetValue(chunkNo, out var chunkEnts)
                         && itemNo < chunkEnts.Value.Lines.Count)
                {
                    chunkEnts.ValueRef.LastUsed = DateTime.UtcNow;
                    return chunkEnts.Value.Lines[itemNo];
                }

                using var bzstream = new BZip2InputStream(bzmemstream);
                using var reader = new StreamReader(bzstream);

                var lines = new List<string>();

                while (reader.ReadLine() is string line)
                {
                    lines.Add(line);
                }

                fileEnts.Entries[chunkNo] = LineCacheLRU.AddFirst((file.FileName, chunkNo, lines, DateTime.UtcNow));

                return itemNo < lines.Count ? lines[itemNo] : null;
            }
        }
    }
}
