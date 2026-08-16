using System.Collections;

namespace EddnIndex.Common.Sectors;

public class HandAuthoredSectorCollection : IEnumerable<HandAuthoredSector>
{
    private readonly List<List<HandAuthoredSector>> _sectors = [];
    private readonly Dictionary<string, (List<HandAuthoredSector> sectors, uint id)> _sectorsByName = [];

    public HandAuthoredSectorCollection() { }

    public HandAuthoredSectorCollection(IEnumerable<HandAuthoredSector> sectors)
    {
        foreach (var sector in sectors)
        {
            Add(sector);
        }
    }

    public void Add(HandAuthoredSector sector)
        => Add(sector.Name, sector.X, sector.Y, sector.Z, sector.Radius, sector.PermitLocked, sector.X0, sector.Y0, sector.Z0, sector.ValidFrom, sector.ValidTo);

    public void Add(string name, decimal x, decimal y, decimal z, decimal radius, bool permitlocked = false, decimal? x0 = null, decimal? y0 = null, decimal? z0 = null, DateTime? validFrom = null, DateTime? validTo = null)
    {
        if (!_sectorsByName.TryGetValue(name, out var sectors))
        {
            uint id = (uint)_sectors.Count + 1;
            sectors = (new List<HandAuthoredSector>(), id);
            _sectors.Add(sectors.sectors);
            _sectorsByName[name] = sectors;
        }

        sectors.sectors.Add(new HandAuthoredSector(
            id: sectors.id,
            name: name,
            x: x,
            y: y,
            z: z,
            permitLocked: permitlocked,
            radius: radius,
            x0: x0 ?? (x - radius),
            y0: y0 ?? (y - radius),
            z0: z0 ?? (z - radius),
            validFrom: validFrom ?? new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            validTo: validTo ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
        ));
    }

    public HandAuthoredSector[] this[int id] => [.. _sectors[id - 1]];

    public bool TryGetSectorId(string name, out ulong id)
    {
        if (_sectorsByName.TryGetValue(name, out var sectors))
        {
            id = sectors.id;
            return true;
        }
        else
        {
            id = 0;
            return false;
        }
    }

    public IEnumerator<HandAuthoredSector> GetEnumerator()
        => _sectors.SelectMany(e => e).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
