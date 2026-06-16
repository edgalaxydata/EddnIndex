using System.Diagnostics.CodeAnalysis;

namespace EddnIndexUpdate.Models;

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

    public int? SectorId => SystemNameId >= (1L << 60) ? (int)(SystemNameId >> 40) - 0x100000 : null;

    public int? SectorAddress => SystemNameId >= 0 && SystemNameId < (1L << 60) ? (int)(SystemNameId >> 40) : null;

    public string? PGSuffix => SystemHelpers.GetPGSuffix(SystemNameId);

    public bool? IsNamedSystem=> SystemNameId < 0;

    public bool? IsHASystem => SystemNameId >= (1L << 60);

    public long? SystemAddress => SystemHelpers.ModSystemAddressToSystemAddress(ModSystemAddress);

    public string? SysAddr_PGSuffix => SystemHelpers.GetPGSuffix(ModSystemAddress);

    public string? NameSysAddr_PGSuffix => SystemHelpers.GetPGSuffix(NameModSystemAddress);

    public int? SysAddr_SectorAddress => (int?)(ModSystemAddress >> 40);

    public int? NameSysAddr_SectorAddress => (int?)(NameModSystemAddress >> 40);

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
