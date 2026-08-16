namespace EddnIndex.Common.Models;

public record class GameVersionInfo : IHasFirstLastSeen, IHasId<int>
{
    public int Id { get; init; }
    public string? GameVersion { get; init; }
    public string? GameBuild { get; init; }
    public bool? IsOdyssey { get; init; }
    public bool? IsHorizons { get; init; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }

    public virtual bool Equals(GameVersionInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return GameVersion == other.GameVersion
            && GameBuild == other.GameBuild
            && IsOdyssey == other.IsOdyssey
            && IsHorizons == other.IsHorizons;
    }

    public override int GetHashCode()
        => HashCode.Combine(GameVersion, GameBuild, IsOdyssey, IsHorizons);
}
