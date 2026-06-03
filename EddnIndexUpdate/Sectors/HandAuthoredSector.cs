namespace EddnIndexUpdate.Sectors;

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

    public HandAuthoredSector(string Name, decimal X, decimal Y, decimal Z, decimal Radius, bool? PermitLocked = null, decimal? X0 = null, decimal? Y0 = null, decimal? Z0 = null, DateTime? ValidFrom = null, DateTime? ValidTo = null, uint Id = 0)
    {
        this.Id = Id;
        this.Name = Name;
        this.X = X;
        this.Y = Y;
        this.Z = Z;
        this.Radius = Radius;
        this.X0 = X0 ?? X - Radius;
        this.Y0 = Y0 ?? Y - Radius;
        this.Z0 = Z0 ?? Z - Radius;
        this.PermitLocked = PermitLocked ?? false;
        this.ValidFrom = ValidFrom ?? new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        this.ValidTo = ValidTo ?? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
    }

    public (uint x, uint y, uint z) GetBaseBlockCoords(int masscode)
    {
        uint mult = 10U * (1U << 7 - masscode);
        return ( (uint)((X0 + 49985) / mult), (uint)((Y0 + 40985) / mult), (uint)((Z0 + 24105) / mult) );
    }
}
