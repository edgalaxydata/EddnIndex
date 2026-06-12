namespace EddnIndexUpdate.Models;

public record class BodyInfo : IHasFirstLastSeen, IHasId<long>
{
    public long Id { get; set; }
    public int SystemId { get; set; }
    public long? SystemNameId { get; init; }
    public int? BodyId { get; init; }
    public int? ParentSetId { get; set; }
    public int? BodyNameId { get; init; }
    public int? BodyDesignationId { get; set; }
    public decimal? ArgOfPeriapsis { get; init; }
    public decimal? Inclination { get; init; }
    public decimal? SemiMajorAxis { get; init; }
    public sbyte SemiMajorAxisScale { get; init; }
    public bool? IsRejected { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }

    public SystemInfo? System { get; set; }
    public ParentSet? ParentSet { get; set; }

    public int? SysName_SectorId
    {
        get => SystemNameId >= (1L << 60) ? (int)(SystemNameId >> 40) - 0x100000 : null;
        private set;
    }

    public int? SysName_SectorAddress
    {
        get => SystemNameId >= 0 && SystemNameId < (1L << 60) ? (int)(SystemNameId >> 40) : null;
        private set;
    }

    public string? SysName_PGSuffix
    {
        get => Models.SystemInfo.GetPGSuffix(SystemNameId);
        private set;
    }

    public virtual bool Equals(BodyInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return this.SystemId == other.SystemId
            && this.ParentSetId == other.ParentSetId
            && this.BodyNameId == other.BodyNameId
            && this.ArgOfPeriapsis == other.ArgOfPeriapsis
            && this.Inclination == other.Inclination;
    }

    public bool? ArgOfPeriapsisEquals(decimal? other)
    {
        if (this.ArgOfPeriapsis is decimal thisVal && other is decimal otherVal)
        {
            return Math.Abs((thisVal + 360) % 360 - (otherVal + 360) % 360) < 1;
        }

        return null;
    }

    public bool? InclinationEquals(decimal? other)
    {
        if (this.Inclination is decimal thisVal && other is decimal otherVal)
        {
            return Math.Abs((thisVal + 360) % 360 - (otherVal + 360) % 360) < 1;
        }

        return null;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SystemId, ParentSetId, BodyNameId, ArgOfPeriapsis, Inclination);
    }
}
