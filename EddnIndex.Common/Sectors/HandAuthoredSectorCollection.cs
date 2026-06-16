using System.Collections;

namespace EddnIndex.Common.Sectors;

public class HandAuthoredSectorCollection : IEnumerable<HandAuthoredSector>
{
    private readonly List<List<HandAuthoredSector>> m_Sectors = [];
    private readonly Dictionary<string, (List<HandAuthoredSector> sectors, uint id)> m_SectorsByName = [];

    public HandAuthoredSectorCollection() { }

    public HandAuthoredSectorCollection(IEnumerable<HandAuthoredSector> sectors)
    {
        foreach (var sector in sectors)
        {
            Add(sector);
        }
    }

    public void Add(HandAuthoredSector sector)
    {
        Add(sector.Name, sector.X, sector.Y, sector.Z, sector.Radius, sector.PermitLocked, sector.X0, sector.Y0, sector.Z0, sector.ValidFrom, sector.ValidTo);
    }

    public void Add(string name, decimal x, decimal y, decimal z, decimal radius, bool permitlocked = false, decimal? x0 = null, decimal? y0 = null, decimal? z0 = null, DateTime? validFrom = null, DateTime? validTo = null)
    {
        if (!m_SectorsByName.TryGetValue(name, out var sectors))
        {
            uint id = (uint)m_Sectors.Count + 1;
            sectors = (new List<HandAuthoredSector>(), id);
            m_Sectors.Add(sectors.sectors);
            m_SectorsByName[name] = sectors;
        }

        sectors.sectors.Add(new HandAuthoredSector(
            Id: sectors.id,
            Name: name,
            X: x,
            Y: y,
            Z: z,
            PermitLocked: permitlocked,
            Radius: radius,
            X0: x0 ?? x - radius,
            Y0: y0 ?? y - radius,
            Z0: z0 ?? z - radius,
            ValidFrom: validFrom ?? new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidTo: validTo ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
        ));
    }

    public HandAuthoredSector[] this[int id] => [.. m_Sectors[id - 1]];

    public bool TryGetSectorId(string name, out ulong id)
    {
        if (m_SectorsByName.TryGetValue(name, out var sectors))
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
    {
        return m_Sectors.SelectMany(e => e).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
