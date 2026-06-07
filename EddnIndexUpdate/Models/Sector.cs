namespace EddnIndexUpdate.Models;

public record class Sector : IHasId<int>, IHasFirstLastSeen
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public decimal? X0 { get; init; }
    public decimal? Y0 { get; init; }
    public decimal? Z0 { get; init; }
    public decimal? SizeX { get; init; }
    public decimal? SizeY { get; init; }
    public decimal? SizeZ { get; init; }
    public int? SectorAddress { get; init; }
    public bool? IsHASector { get; init; }
    public int? HASectorPriority { get; init; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }

    public virtual bool Equals(Sector? other) => other?.Name == this.Name;

    public override int GetHashCode() => Name.GetHashCode();
}
