using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using EddnIndex.Common;
using EddnIndex.Common.Sectors;
using Microsoft.EntityFrameworkCore;
using Models = EddnIndex.Common.Models;

namespace EddnIndexUpdate;

public partial class FileProcessor
{
    private readonly Dictionary<(string? SystemName, long? SystemAddress, decimal? X, decimal? Y, decimal? Z), Models.SystemInfo> _systemCache = [];
    private readonly Dictionary<int, Models.SystemInfo> _systemCacheById = [];

    private readonly Dictionary<string, Models.SystemName> _systemNames = [];
    private readonly Dictionary<int, Models.SystemName> _systemNamesById = [];
    private readonly Dictionary<string, Models.Sector> _sectors = [];
    private readonly Dictionary<int, Models.Sector> _sectorsById = [];
    private readonly Dictionary<int, Models.Sector> _sectorsByAddr = [];

    private async Task Init_SystemsAsync(CancellationToken canceltoken)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

        if (_sectors.Count == 0 || _sectorsById.Count == 0)
        {
            _logger.LogLoadingSectors();

            foreach (var sector in ctx.Set<Models.Sector>().AsNoTracking())
            {
                _sectors[sector.Name] = sector;
            }

            foreach (var hagrp in HandAuthoredSectors.Sectors.GroupBy(e => (e.Name, e.X0, e.Y0, e.Z0, e.ValidFrom, e.ValidTo)))
            {
                var (name, x0, y0, z0, validFrom, validTo) = hagrp.Key;
                decimal sizeX = hagrp.Max(e => e.X + e.Radius) - x0;
                decimal sizeY = hagrp.Max(e => e.Y + e.Radius) - y0;
                decimal sizeZ = hagrp.Max(e => e.Z + e.Radius) - z0;
                int haSectorPriority = (int)hagrp.Min(e => e.Id);

                if (!_sectors.ContainsKey(name))
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

                    _sectors[sector.Name] = sector;
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
                                int x = (x0 * xm) + xo + 39;
                                int y = (y0 * ym) + yo + 32;
                                int z = (z0 * zm) + zo + 39;

                                if (swapxz)
                                {
                                    (x, z) = (z, x);
                                }

                                if (!gotSectors.Contains((x, y, z)))
                                {
                                    int sectorAddress = x + (y << 7) + (z << 13);
                                    string sectorName = PGSectors.GetSectorName(sectorAddress);

                                    if (!_sectors.ContainsKey(sectorName))
                                    {
                                        var sector = new Models.Sector
                                        {
                                            Name = sectorName,
                                            X0 = (x * 1280) - 49985,
                                            Y0 = (y * 1280) - 40985,
                                            Z0 = (z * 1280) - 24105,
                                            IsHASector = false,
                                            SectorAddress = sectorAddress,
                                            SizeX = 1280,
                                            SizeY = 1280,
                                            SizeZ = 1280,
                                            ValidFrom = new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                            ValidTo = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
                                        };

                                        ctx.Add(sector);
                                        _sectors[sectorName] = sector;
                                    }

                                    gotSectors.Add((x, y, z));
                                }
                            }
                        }
                    }
                }
            }

            await ctx.SaveChangesAsync(canceltoken);

            foreach (var sector in _sectors.Values)
            {
                _sectorsById[sector.Id] = sector;

                if (sector.SectorAddress is int sectorAddr)
                {
                    _sectorsByAddr[sectorAddr] = sector;
                }
            }
        }

        if (_systemNames.Count == 0 || _systemNamesById.Count == 0)
        {
            foreach (var sysname in ctx.Set<Models.SystemName>().AsNoTracking())
            {
                _systemNames[sysname.Name] = sysname;
                _systemNamesById[sysname.Id] = sysname;
            }
        }
    }

    private async Task<Models.Sector> GetOrAddSectorAsync(string name, CancellationToken canceltoken)
    {
        if (_sectors.TryGetValue(name, out var sector)) return sector;

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

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
                X0 = ((sectorid & 0x7F) * 1280) - 49985,
                Y0 = (((sectorid >> 7) & 0x3F) * 1280) - 40985,
                Z0 = (((sectorid >> 13) & 0x7F) * 1280) - 24105,
                IsHASector = false
            };
        }

        ctx.Add(sector);
        await ctx.SaveChangesAsync(canceltoken);
        _sectors[name] = sector;
        return sector;
    }

    [return: NotNullIfNotNull(nameof(name))]
    private async Task<long?> GetOrAddSystemNameAsync(string? name, CancellationToken canceltoken)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        if (SystemHelpers.TrySplitProcgenName(name, out string? sectorName, out int mid, out int n2, out int masscode)
            && n2 is >= 0 and < 65536
            && mid is >= 0 and < 0x200000
            && masscode is >= 0 and < 8)
        {
            long boxelid = (long)n2 | ((long)mid << 16) | ((long)masscode << 37);
            string checkSuffix = SystemHelpers.GetPGSuffix(boxelid);
            Assert(name.EndsWith(checkSuffix), extraData: new { name, checkSuffix });

            var sector = await GetOrAddSectorAsync(sectorName, canceltoken);

            if (sector.SectorAddress is int sectoraddr && sectoraddr >= 0 && sectoraddr < 0x100000)
            {
                return ((long)sectoraddr << 40) | boxelid;
            }

            return (((long)sector.Id + 0x100000) << 40) | boxelid;
        }

        if (_systemNames.TryGetValue(name, out var systemname))
        {
            return -systemname.Id;
        }

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);
        systemname = new Models.SystemName { Name = name };
        ctx.Add(systemname);
        await ctx.SaveChangesAsync(canceltoken);

        _systemNames[name] = systemname;
        _systemNamesById[systemname.Id] = systemname;

        return -systemname.Id;
    }

    private long? TryGetNameModSystemAddress(long? nameid)
    {
        if (nameid is not long nameId || nameId < 0)
        {
            return null;
        }

        if (nameId < 0x1000_0000_0000_0000)
        {
            return nameId;
        }

        long n2 = nameId & 0xFFFF;
        long mid = (nameId >> 16) & 0x1FFFFF;
        int masscode = (int)((nameId >> 37) & 7);
        int sectorid = (int)((nameId >> 40) - 0x100000);

        if (!_sectorsById.TryGetValue(sectorid, out var sector)
            || sector.X0 == null
            || sector.Y0 == null
            || sector.Z0 == null)
        {
            return null;
        }

        int x0 = (int)((sector.X0 + 49985) / (10 << masscode));
        int y0 = (int)((sector.Y0 + 40985) / (10 << masscode));
        int z0 = (int)((sector.Z0 + 24105) / (10 << masscode));
        long xv = (mid & 0x7F) + x0;
        long yv = ((mid >> 7) & 0x7F) + y0;
        long zv = ((mid >> 14) & 0x7F) + z0;
        mid = (xv & (0x7F >> masscode)) | ((yv & (0x7F >> masscode)) << 7) | ((zv & (0x7F >> masscode)) << 14);
        long sectorAddr = (xv >> (7 - masscode)) | ((yv >> (7 - masscode)) << 7) | ((zv >> (7 - masscode)) << 13);
        return n2 | (mid << 16) | ((long)masscode << 37) | (sectorAddr << 40);
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

        if (_systemCache.TryGetValue((name, systemAddress, x, y, z), out var system))
        {
            return system;
        }

        long? nameid = await GetOrAddSystemNameAsync(name, canceltoken);
        long? modsysaddr = SystemHelpers.SystemAddressToModSystemAddress(systemAddress);
        long? revsysaddr = SystemHelpers.ModSystemAddressToSystemAddress(modsysaddr);
        Assert(systemAddress == revsysaddr, extraData: new { modsysaddr, systemAddress, revsysaddr });
        long? namemodsysaddr = TryGetNameModSystemAddress(nameid);

        DateTime? validFrom = null;
        DateTime? validTo = null;

        if (name != null && _systemNameOverrides.TryGetValue(name, out var overrides))
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

        long? namesysaddr = SystemHelpers.ModSystemAddressToSystemAddress(namemodsysaddr);
        long? revnamemodsysaddr = SystemHelpers.SystemAddressToModSystemAddress(namesysaddr);
        Assert(namemodsysaddr == revnamemodsysaddr, extraData: new { namemodsysaddr, namesysaddr, revnamemodsysaddr });

        systemAddress ??= namesysaddr;

        await using var ctx = await _contextFactory.CreateDbContextAsync(canceltoken);

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
            if (!_systemCacheById.TryGetValue(system.Id, out var byid))
            {
                _systemCacheById[system.Id] = byid = system;
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

        _systemCache.Add((name, systemAddress, x, y, z), system);

        if (systemAddress != null && modsysaddr == namemodsysaddr)
        {
            _systemCache.Add((name, null, x, y, z), system);
        }

        return system;
    }
}
