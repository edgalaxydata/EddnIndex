namespace EddnIndex.Common.Models;

public record class SoftwareInfo : IHasFirstLastSeen, IHasId<int>
{
    public int Id { get; init; }
    public required string SoftwareName { get; init; }
    public required string SoftwareVersion { get; init; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }

    public virtual bool Equals(SoftwareInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return SoftwareName == other.SoftwareName
            && SoftwareVersion == other.SoftwareVersion;
    }

    public override int GetHashCode()
        => HashCode.Combine(SoftwareName, SoftwareVersion);
}
