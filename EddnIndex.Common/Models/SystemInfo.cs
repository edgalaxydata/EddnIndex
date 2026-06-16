namespace EddnIndex.Common.Models;

public record class SystemInfo : IHasFirstLastSeen, IHasId<int>
{
    public int Id { get; set; }
    public long? SystemNameId { get; init; }
    public long? ModSystemAddress { get; init; }
    public long? NameModSystemAddress { get; init; }
    public decimal? X { get; init; }
    public decimal? Y { get; init; }
    public decimal? Z { get; init; }
    public bool? IsRejected { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }

    public int? SectorId
    {
        get => EDDNContext.UseComputedFields ? (SystemNameId >= (1L << 60) ? (int)(SystemNameId >> 40) - 0x100000 : null) : field;
        private set;
    }

    public int? SectorAddress
    {
        get => EDDNContext.UseComputedFields ? (SystemNameId >= 0 && SystemNameId < (1L << 60) ? (int)(SystemNameId >> 40) : null) : field;
        private set;
    }

    public string? PGSuffix
    {
        get => EDDNContext.UseComputedFields ? (SystemHelpers.GetPGSuffix(SystemNameId)) : field;
        private set;
    }

    public bool? IsNamedSystem
    {
        get => EDDNContext.UseComputedFields ? (SystemNameId < 0) : field;
        private set;
    }

    public bool? IsHASystem
    {
        get => EDDNContext.UseComputedFields ? (SystemNameId >= (1L << 60)) : field;
        private set;
    }

    public long? SystemAddress
    {
        get => EDDNContext.UseComputedFields ? (SystemHelpers.ModSystemAddressToSystemAddress(ModSystemAddress)) : field;
        private set;
    }

    public string? SysAddr_PGSuffix
    {
        get => EDDNContext.UseComputedFields ? (SystemHelpers.GetPGSuffix(ModSystemAddress)) : field;
        private set;
    }

    public string? NameSysAddr_PGSuffix
    {
        get => EDDNContext.UseComputedFields ? (SystemHelpers.GetPGSuffix(NameModSystemAddress)) : field;
        private set;
    }

    public int? SysAddr_SectorAddress
    {
        get => EDDNContext.UseComputedFields ? ((int?)(ModSystemAddress >> 40)) : field;
        private set;
    }

    public int? NameSysAddr_SectorAddress
    {
        get => EDDNContext.UseComputedFields ? ((int?)(NameModSystemAddress >> 40)) : field;
        private set;
    }

    public virtual bool Equals(SystemInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(other, this)) return true;

        return this.SystemNameId == other.SystemNameId
            && this.ModSystemAddress == other.ModSystemAddress
            && this.X == other.X
            && this.Y == other.Y
            && this.Z == other.Z;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SystemNameId, ModSystemAddress, X, Y, Z);
    }
}
