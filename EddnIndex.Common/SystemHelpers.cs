using System.Diagnostics.CodeAnalysis;

namespace EddnIndex.Common;

public static class SystemHelpers
{
    [return: NotNullIfNotNull(nameof(modSystemAddress))]
    public static string? GetPGSuffix(long? modSystemAddress, bool includeN2 = true)
    {
        if (modSystemAddress is not long msa || msa < 0) return null;

        int masscode = (int)((msa >> 37) & 7);
        int mid = (int)((msa >> 16) & 0x1FFFFF);
        long n2 = msa & 0xFFFF;

        return GetPGSuffix(masscode, mid, n2, includeN2);
    }

    public static string GetPGSuffix(int masscode, int mid, long n2, bool includeN2 = true)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(masscode, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(masscode, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(mid, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(mid, 1 << 21);
        ArgumentOutOfRangeException.ThrowIfLessThan(n2, 0);

        (mid, int l1) = Math.DivRem(mid, 26);
        (mid, int l2) = Math.DivRem(mid, 26);
        (int n1, int l3) = Math.DivRem(mid, 26);

        return string.Concat(
            ' ',
            (char)(l1 + 'A'),
            (char)(l2 + 'A'),
            '-',
            (char)(l3 + 'A'),
            ' ',
            (char)(masscode + 'a'),
            n1 > 0 ? $"{n1}-" : "",
            includeN2 ? n2.ToString() : ""
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

        masscode = toLower(sn[i]) - 'a';
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
        mid += toUpper(sn[i]) - 'A';
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

    public static void SplitSystemAddress(long sysaddr, out int masscode, out int mid, out int sectorAddr, out long n2, out int bodyId)
    {
        bodyId = (int)(sysaddr >> 55) & 0x1FF;

        sysaddr &= 0x007F_FFFF_FFFF_FFFF;

        masscode = (int)(sysaddr & 7);
        long zv = (sysaddr >> 3) & (0x3FFF >> masscode);
        long yv = (sysaddr >> (17 - masscode)) & (0x1FFF >> masscode);
        long xv = (sysaddr >> (30 - (masscode * 2))) & (0x3FFF >> masscode);
        n2 = sysaddr >> (44 - (masscode * 3));
        mid = (int)((xv & (0x7F >> masscode)) | ((yv & (0x7F >> masscode)) << 7) | ((zv & (0x7F >> masscode)) << 14));
        sectorAddr = (int)((xv >> (7 - masscode)) | ((yv >> (7 - masscode)) << 7) | ((zv >> (7 - masscode)) << 13));
    }

    public static long MergeSystemAddress(int masscode, int mid, int sectorAddr, long n2, int bodyId)
    {
        long xv = ((mid >> 0) & 0x7F) + (((sectorAddr >> 0) & 0x7F) << (7 - masscode));
        long yv = ((mid >> 7) & 0x7F) + (((sectorAddr >> 7) & 0x3F) << (7 - masscode));
        long zv = ((mid >> 14) & 0x7F) + (((sectorAddr >> 13) & 0x7F) << (7 - masscode));

        return unchecked((long)bodyId << 55)
             | ((long)n2 << (44 - (masscode * 3)))
             | (xv << (30 - (masscode * 2)))
             | (yv << (17 - (masscode * 1)))
             | (zv << (3 - (masscode * 0)))
             | (long)masscode;
    }

    public static long? SystemAddressToModSystemAddress(long? systemAddress)
    {
        if (systemAddress is not long sysaddr || sysaddr < 0)
        {
            return null;
        }

        sysaddr &= 0x007F_FFFF_FFFF_FFFF;

        SplitSystemAddress(sysaddr, out int masscode, out int mid, out int sectorAddr, out long n2, out _);

        if (n2 > 65536) return null;

        return n2 | ((long)mid << 16) | ((long)masscode << 37) | ((long)sectorAddr << 40);
    }

    public static long? ModSystemAddressToSystemAddress(long? modSystemAddress)
    {
        if (modSystemAddress is not long msa || modSystemAddress < 0) return null;

        int masscode = (int)((msa >> 37) & 7);
        long n2 = msa & 0xFFFF;
        int mid = (int)((msa >> 16) & 0x1FFFFF);
        int sectorAddr = (int)(msa >> 40);
        return MergeSystemAddress(masscode, mid, sectorAddr, n2, 0);
    }
}
