namespace EddnIndex.Common.Models;

public record class StationInfo : IHasFirstLastSeen, IHasId<int>
{
    public int Id { get; set; }
    public long? MarketId { get; init; }
    public string? SystemName { get; init; }
    public string? StationName { get; init; }
    public string? StationType { get; init; }
    public string? BodyName { get; init; }
    public long? SystemAddress { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public bool? IsRejected { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }

    public virtual bool Equals(StationInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return MarketId == other.MarketId
            && SystemName == other.SystemName
            && StationName == other.StationName
            && SystemAddress == other.SystemAddress;
    }

    public override int GetHashCode()
        => HashCode.Combine(MarketId, SystemName, StationName, SystemAddress);
}
