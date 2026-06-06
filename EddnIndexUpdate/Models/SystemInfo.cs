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

    public int? SectorId
    {
        get => SystemNameId >= (1L << 60) ? (int)(SystemNameId >> 40) - 0x100000 : null;
        set { }
    }

    public int? SectorAddress
    {
        get => SystemNameId >= 0 && SystemNameId < (1L << 60) ? (int)(SystemNameId >> 40) : null;
        set { }
    }

    public string? PGSuffix
    {
        get => GetPGSuffix(SystemNameId);
        set { }
    }

    public bool? IsNamedSystem
    {
        get => SystemNameId < 0;
        set { }
    }

    public bool? IsHASystem
    {
        get => SystemNameId >= (1L << 60);
        set { }
    }

    public long? SystemAddress
    {
        get => ModSystemAddressToSystemAddress(ModSystemAddress);
        set { }
    }

    public string? SysAddr_PGSuffix
    {
        get => GetPGSuffix(ModSystemAddress);
        set { }
    }

    public string? NameSysAddr_PGSuffix
    {
        get => GetPGSuffix(NameModSystemAddress);
        set { }
    }

    public int? SysAddr_SectorAddress
    {
        get => (int?)(ModSystemAddress >> 40);
        set { }
    }

    public int? NameSysAddr_SectorAddress
    {
        get => (int?)(NameModSystemAddress >> 40);
        set { }
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

    [return: NotNullIfNotNull(nameof(modSystemAddress))]
    public static string? GetPGSuffix(long? modSystemAddress)
    {
        if (modSystemAddress is not long msa || msa < 0) return null;

        var masscode = (msa >> 37) & 7;
        var mid = (msa >> 16) & 0x1FFFFF;
        var n2 = msa & 0xFFFF;

        return string.Concat(
            ' ',
            (char)((mid % 26) + 'A'),
            (char)(((mid / 26) % 26) + 'A'),
            '-',
            (char)(((mid / (26 * 26)) % 26) + 'A'),
            ' ',
            (char)(masscode + 'a'),
            mid >= 26 * 26 * 26 ? (mid / (26 * 26 * 26)).ToString() + "-" : "",
            n2
        );
    }

    public static bool TrySplitProcgenName(ReadOnlySpan<char> systemname, [NotNullWhen(true)] out string? sectorname, out int mid, out int n2, out int masscode, bool caseSensitive = true)
    {
        Func<char, bool> isLetterUpper = caseSensitive ? char.IsAsciiLetterUpper : char.IsAsciiLetter;
        Func<char, char> toUpper = caseSensitive ? e => e : char.ToUpperInvariant;
        Func<char, char> toLower = caseSensitive ? e => e : char.ToLowerInvariant;

        var sn = systemname;

        int i = sn.Length - 1;

        if (i < 9) goto fail;                                   // a bc-d e0

        if (!char.IsAsciiDigit(sn[i])) goto fail;               // cepheus dark region a sector xy-z a1-[0]

        n2 = 0;
        int mult = 1;
        while (i > 8 && char.IsAsciiDigit(sn[i]))
        {
            n2 += (sn[i] - '0') * mult;
            i--;
            mult *= 10;
        }

        mid = 0;
        if (sn[i] == '-')                                       // cepheus dark region a sector xy-z a1[-]0
        {
            i--;

            int vend = i;
            mult = 1;
            while (i > 8 && char.IsAsciiDigit(sn[i]))           // cepheus dark region a sector xy-z a[1]-0
            {
                mid += (sn[i] - '0') * mult;
                i--;
                mult *= 10;
            }

            if (i == vend) goto fail;
        }

        mid *= 26 * 26 * 26;

        masscode = (toLower(sn[i]) - 'a');
        if (masscode is < 0 or > 7) goto fail;                  // cepheus dark region a sector xy-z [a]1-0
        i--;
        if (sn[i] != ' ') goto fail;                            // cepheus dark region a sector xy-z[ ]a1-0
        i--;
        if (!isLetterUpper(sn[i])) goto fail;                   // cepheus dark region a sector xy-[z] a1-0
        mid += (toUpper(sn[i]) - 'A') * 26 * 26;
        i--;
        if (sn[i] != '-') goto fail;                            // cepheus dark region a sector xy[-]z a1-0
        i--;
        if (!isLetterUpper(sn[i])) goto fail;                   // cepheus dark region a sector x[y]-z a1-0
        mid += (toUpper(sn[i]) - 'A') * 26;
        i--;
        if (!isLetterUpper(sn[i])) goto fail;                   // cepheus dark region a sector [x]y-z a1-0
        mid += (toUpper(sn[i]) - 'A');
        i--;
        if (sn[i] != ' ') goto fail;                            // cepheus dark region a sector[ ]xy-z a1-0
        sectorname = new string(systemname[..i]);               // [cepheus dark region a sector] xy-z a1-0
        return true;

    fail:
        sectorname = null;
        mid = 0;
        n2 = 0;
        masscode = 0;
        return false;
    }

    public static long? SystemAddressToModSystemAddress(long? systemAddress)
    {
        if (systemAddress is not long sysaddr || sysaddr < 0)
        {
            return null;
        }

        sysaddr &= 0x007F_FFFF_FFFF_FFFF;

        var masscode = (int)(sysaddr & 7);
        var zv = (sysaddr >> 3) & (0x3FFF >> masscode);
        var yv = (sysaddr >> (17 - masscode)) & (0x1FFF >> masscode);
        var xv = (sysaddr >> (30 - masscode * 2)) & (0x3FFF >> masscode);
        var n2 = sysaddr >> (44 - masscode * 3);
        var mid = (xv & (0x7F >> masscode)) | ((yv & (0x7F >> masscode)) << 7) | ((zv & (0x7F >> masscode)) << 14);
        var sectorAddr = (xv >> (7 - masscode)) | ((yv >> (7 - masscode)) << 7) | ((zv >> (7 - masscode)) << 13);

        if (n2 > 65536) return null;

        return n2 | (mid << 16) | ((long)masscode << 37) | (sectorAddr << 40);
    }

    public static long? ModSystemAddressToSystemAddress(long? modSystemAddress)
    {
        if (modSystemAddress is not long msa || modSystemAddress < 0) return null;

        var masscode = (int)((msa >> 37) & 7);
        var n2 = msa & 0xFFFF;
        var xv = ((msa >> 16) & 0x7F) + (((msa >> 40) & 0x7F) << (7 - masscode));
        var yv = ((msa >> 23) & 0x7F) + (((msa >> 47) & 0x3F) << (7 - masscode));
        var zv = ((msa >> 30) & 0x7F) + (((msa >> 53) & 0x7F) << (7 - masscode));

        return n2 << (44 - masscode * 3)
             | (xv << (30 - masscode * 2))
             | (yv << (17 - masscode * 1))
             | (zv << (3 - masscode * 0))
             | (long)masscode;
    }
}
