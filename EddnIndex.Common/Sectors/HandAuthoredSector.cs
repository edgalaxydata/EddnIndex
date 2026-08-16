namespace EddnIndex.Common.Sectors;

public readonly record struct HandAuthoredSector
{
    public uint Id { get; }
    public string Name { get; }
    public decimal X { get; }
    public decimal Y { get; }
    public decimal Z { get; }
    public decimal Radius { get; }
    public decimal X0 { get; }
    public decimal Y0 { get; }
    public decimal Z0 { get; }
    public bool PermitLocked { get; }
    public DateTime ValidFrom { get; }
    public DateTime ValidTo { get; }

    public HandAuthoredSector(string name, decimal x, decimal y, decimal z, decimal radius, bool? permitLocked = null, decimal? x0 = null, decimal? y0 = null, decimal? z0 = null, DateTime? validFrom = null, DateTime? validTo = null, uint id = 0)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;
        Z = z;
        Radius = radius;
        X0 = x0 ?? (x - radius);
        Y0 = y0 ?? (y - radius);
        Z0 = z0 ?? (z - radius);
        PermitLocked = permitLocked ?? false;
        ValidFrom = validFrom ?? new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        ValidTo = validTo ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
    }

    public (uint x, uint y, uint z) GetBaseBlockCoords(int masscode)
    {
        uint mult = 10U * (1U << (7 - masscode));
        return ((uint)((X0 + 49985) / mult), (uint)((Y0 + 40985) / mult), (uint)((Z0 + 24105) / mult));
    }
}
