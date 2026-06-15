using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace EddnIndexUpdate.Sectors;

public static class PGSectors
{
    private const int OrdBits = 7;
    private const int XBits = 7;
    private const int YBits = 6;
    private const int ZBits = 7;
    private const int OrdMask = (1 << OrdBits) - 1;
    private const int XMask = (1 << XBits) - 1;
    private const int YMask = (1 << YBits) - 1;
    private const int ZMask = (1 << ZBits) - 1;
    private const int XShift = 0;
    private const int YShift = XShift + XBits;
    private const int ZShift = YShift + YBits;
    private const int OrdXShift = 0;
    private const int OrdYShift = OrdXShift + OrdBits;
    private const int OrdZShift = OrdYShift + OrdBits;
    private const int OrdXStride = 1;
    private const int OrdYStride = OrdXStride << OrdBits;
    private const int OrdZStride = OrdYStride << OrdBits;
    private const int XStride = 1;
    private const int YStride = XStride << XBits;
    private const int ZStride = YStride << YBits;
    private const int MaxOrd = (1 << (OrdBits * 3)) - 1;
    private const int MaxSectorId = (1 << (XBits + YBits + ZBits)) - 1;

    private const uint _mask8x3u8 = 0b000_000_000_000_000_011_111_111U;
    private const uint _mask4x3u8 = 0b000_000_001_111_000_000_001_111U;
    private const uint _mask2x3u8 = 0b000_011_000_011_000_011_000_011U;
    private const uint _mask1x3u8 = 0b001_001_001_001_001_001_001_001U;

    private const uint _mask16x2u16 = 0b0000_0000_0000_0000_1111_1111_1111_1111U;
    private const uint _mask8x2u16  = 0b0000_0000_1111_1111_0000_0000_1111_1111U;
    private const uint _mask4x2u16  = 0b0000_1111_0000_1111_0000_1111_0000_1111U;
    private const uint _mask2x2u16  = 0b0011_0011_0011_0011_0011_0011_0011_0011U;
    private const uint _mask1x2u16  = 0b0101_0101_0101_0101_0101_0101_0101_0101U;

    public readonly record struct ByteXYZ(sbyte X, sbyte Y, sbyte Z) : IComparable<ByteXYZ>
    {
        public readonly bool IsValid => X >= 0 && X <= XMask
                                     && Y >= 0 && Y <= YMask
                                     && Z >= 0 && Z <= ZMask;

        public readonly int Ord => IsValid
                                 ? X * OrdXStride + Y * OrdYStride + Z * OrdZStride
                                 : -1;

        public readonly int SectorId => IsValid
                                      ? X * XStride + Y * YStride + Z * ZStride
                                      : -1;

        public override readonly string ToString() => $"({X},{Y},{Z})";

        public readonly int CompareTo(ByteXYZ other)
        {
            return (X, Y, Z).CompareTo((other.X, other.Y, other.Z));
        }

        public static readonly ByteXYZ Invalid = new(sbyte.MinValue, sbyte.MinValue, sbyte.MinValue);

        public static ByteXYZ FromSectorId(int sectorid)
            => new(
                (sbyte)(sectorid >> XShift & XMask),
                (sbyte)(sectorid >> YShift & YMask),
                (sbyte)(sectorid >> ZShift & ZMask)
            );

        public static ByteXYZ FromOrdinal(int ordinal)
            => new(
                (sbyte)(ordinal >> OrdXShift & OrdMask),
                (sbyte)(ordinal >> OrdYShift & OrdMask),
                (sbyte)(ordinal >> OrdZShift & OrdMask)
            );

        public static ByteXYZ FromOrdinal(uint ordinal)
            => new(
                (sbyte)(ordinal >> OrdXShift & OrdMask),
                (sbyte)(ordinal >> OrdYShift & OrdMask),
                (sbyte)(ordinal >> OrdZShift & OrdMask)
            );
    }

    private readonly record struct FragmentInfo(
            string Value,
            bool IsPrefix = false,
            bool IsVowelPrefix = false,
            int PrefixIndex = 0,
            int PrefixOffset = 0,
            int PrefixRunLength = 0,
            bool IsInfix = false,
            bool IsVowelInfix = false,
            int InfixIndex = 0,
            int InfixOffset = 0,
            int InfixRunLength = 0,
            bool IsSuffix = false,
            bool IsVowelSuffix = false,
            int SuffixIndex = 0
    );

    // Tables of prefixes, infixes and suffixes from https://bitbucket.org/Esvandiary/edts/src/develop/pgdata.py
    // Prefixes
    private static readonly string[] Prefixes =
    [
        "Th",  "Eo",  "Oo",  "Eu",  "Tr",  "Sly", "Dry", "Ou",
        "Tz",  "Phl", "Ae",  "Sch", "Hyp", "Syst","Ai",  "Kyl",
        "Phr", "Eae", "Ph",  "Fl",  "Ao",  "Scr", "Shr", "Fly",
        "Pl",  "Fr",  "Au",  "Pry", "Pr",  "Hyph","Py",  "Chr",
        "Phyl","Tyr", "Bl",  "Cry", "Gl",  "Br",  "Gr",  "By",
        "Aae", "Myc", "Gyr", "Ly",  "Myl", "Lych","Myn", "Ch",
        "Myr", "Cl",  "Rh",  "Wh",  "Pyr", "Cr",  "Syn", "Str",
        "Syr", "Cy",  "Wr",  "Hy",  "My",  "Sty", "Sc",  "Sph",
        "Spl", "A",   "Sh",  "B",   "C",   "D",   "Sk",  "Io",
        "Dr",  "E",   "Sl",  "F",   "Sm",  "G",   "H",   "I",
        "Sp",  "J",   "Sq",  "K",   "L",   "Pyth","M",   "St",
        "N",   "O",   "Ny",  "Lyr", "P",   "Sw",  "Thr", "Lys",
        "Q",   "R",   "S",   "T",   "Ea",  "U",   "V",   "W",
        "Schr","X",   "Ee",  "Y",   "Z",   "Ei",  "Oe",
    ];

    // Vowelish infixes
    private static readonly string[] VowelInfixes =
    [
        "o",   "ai",  "a",   "oi",  "ea",  "ie",  "u",   "e",
        "ee",  "oo",  "ue",  "i",   "oa",  "au",  "ae",  "oe"
    ];

    // Consonantish infixes
    private static readonly string[] NonVowelInfixes =
    [
        "ll",  "ss",  "b",   "c",   "d",   "f",   "dg",  "g",
        "ng",  "h",   "j",   "k",   "l",   "m",   "n",   "mb",
        "p",   "q",   "gn",  "th",  "r",   "s",   "t",   "ch",
        "tch", "v",   "w",   "wh",  "ck",  "x",   "y",   "z",
        "ph",  "sh",  "ct",  "wr"
    ];

    // Vowelish suffixes
    private static readonly string[] VowelSuffixes =
    [
        "oe",  "io",  "oea", "oi",  "aa",  "ua", "eia", "ae",
        "ooe", "oo",  "a",   "ue",  "ai",  "e",  "iae", "oae",
        "ou",  "uae", "i",   "ao",  "au",  "o",  "eae", "u",
        "aea", "ia",  "ie",  "eou", "aei", "ea", "uia", "oa",
        "aae", "eau", "ee"
    ];

    // Consonantish suffixes
    private static readonly string[] NonVowelSuffixes =
    [
        "b",   "scs", "wsy", "c",   "d",   "vsky","f",   "sms",
        "dst", "g",   "rb",  "h",   "nts", "ch",  "rd",  "rld",
        "k",   "lls", "ck",  "rgh", "l",   "rg",  "m",   "n",
        // Formerly sequence 4/5...
        "hm",  "p",   "hn",  "rk",  "q",   "rl",  "r",   "rm",
        "s",   "cs",  "wyg", "rn",  "ct",  "t",   "hs",  "rbs",
        "rp",  "tts", "v",   "wn",  "ms",  "w",   "rr",  "mt",
        "x",   "rs",  "cy",  "y",   "rt",  "z",   "ws",  "lch", // "y" is speculation
        "my",  "ry",  "nks", "nd",  "sc",  "ng",  "sh",  "nk",
        "sk",  "nn",  "ds",  "sm",  "sp",  "ns",  "nt",  "dy",
        "ss",  "st",  "rrs", "xt",  "nz",  "sy",  "xy",  "rsch",
        "rphs","sts", "sys", "sty", "th",  "tl",  "tls", "rds",
        "nch", "rns", "ts",  "wls", "rnt", "tt",  "rdy", "rst",
        "pps", "tz",  "tch", "sks", "ppy", "ff",  "sps", "kh",
        "sky", "ph",  "lts", "wnst","rth", "ths", "fs",  "pp",
        "ft",  "ks",  "pr",  "ps",  "pt",  "fy",  "rts", "ky",
        "rshch","mly", "py", "bb",  "nds", "wry", "zz",  "nns",
        "ld",  "lf",  "gh",  "lks", "sly", "lk",  "ll",  "rph",
        "ln",  "bs",  "rsts","gs",  "ls",  "vvy", "lt",  "rks",
        "qs",  "rps", "gy",  "wns", "lz",  "nth", "phs"
    ];

    // Vowelish prefixes
    private static readonly HashSet<string> VowelPrefixes = new(
    [
        "Eo",  "Oo",  "Eu",  "Ou",  "Ae",  "Ai",  "Eae", "Ao",
        "Au",  "Aae", "A",   "Io",  "E",   "I",   "O",   "Ea",
        "U",   "Ee",  "Ei",  "Oe"
    ], StringComparer.OrdinalIgnoreCase);

    // Prefixes using short run lengths
    private static readonly Dictionary<string, int> PrefixRunLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Eu",   31 }, { "Sly",   4 }, { "Tz",    1 }, { "Phl",  13 },
        { "Ae",   12 }, { "Hyp",  25 }, { "Kyl",  30 }, { "Phr",  10 },
        { "Eae",   4 }, { "Ao",    5 }, { "Scr",  24 }, { "Shr",  11 },
        { "Fly",  20 }, { "Pry",   3 }, { "Hyph", 14 }, { "Py",   12 },
        { "Phyl",  8 }, { "Tyr",  25 }, { "Cry",   5 }, { "Aae",   5 },
        { "Myc",   2 }, { "Gyr",  10 }, { "Myl",  12 }, { "Lych",  3 },
        { "Myn",  10 }, { "Myr",   4 }, { "Rh",   15 }, { "Wr",   31 },
        { "Sty",   4 }, { "Spl",  16 }, { "Sk",   27 }, { "Sq",    7 },
        { "Pyth",  1 }, { "Lyr",  10 }, { "Sw",   24 }, { "Thr",  32 },
        { "Lys",  10 }, { "Schr",  3 }, { "Z",    34 },
    };

    // Infixes using short run lengths
    private static readonly Dictionary<string, int> InfixRunLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        // Sequence 1
        { "oi",   88 }, { "ue",  147 }, { "oa",   57 },
        { "au",  119 }, { "ae",   12 }, { "oe",   39 },
        // Sequence 2
        { "dg",   31 }, { "tch",  20 }, { "wr",   31 },
    };

    private static readonly FragmentInfo[] Fragments;

    private static readonly List<(string Value, int Offset, int RunLength)> PrefixesByOffset = [];

    private static readonly List<(string Value, int Offset, int RunLength)> VowelInfixesByOffset = [];
    private static readonly List<(string Value, int Offset, int RunLength)> NonVowelInfixesByOffset = [];

    private static readonly Dictionary<ByteXYZ, string> CachedSectorsByCoords = [];
    private static readonly Dictionary<string, ByteXYZ> CachedSectorsByName = new(StringComparer.OrdinalIgnoreCase);

    private static void AddOrUpdateFragment(
            Dictionary<string, FragmentInfo> frags,
            List<(string Value, int Offset, int RunLength)> byOffset,
            string value,
            Dictionary<string, int> runlengths,
            int defaultlen,
            Func<FragmentInfo, string, int, int, FragmentInfo> modifyAction
        )
    {
        var valueLower = value.ToLowerInvariant();

        var frag = frags.TryGetValue(value, out FragmentInfo v)
                 ? v
                 : new FragmentInfo { Value = value };

        if (!runlengths.TryGetValue(value, out int plen))
        {
            plen = defaultlen;
            runlengths[value] = plen;
        }

        frags[valueLower] = modifyAction(frag, valueLower, byOffset.Count, plen);

        byOffset.AddRange(Enumerable.Repeat((value, byOffset.Count, plen), plen));
    }

    private static void AddOrUpdateFragment(
            Dictionary<string, FragmentInfo> frags,
            string value,
            Func<FragmentInfo, string, FragmentInfo> modifyAction
        )
    {
        value = value.ToLowerInvariant();

        var frag = frags.TryGetValue(value, out FragmentInfo v)
                 ? v
                 : new FragmentInfo { Value = value };

        frags[value] = modifyAction(frag, value);
    }

    static PGSectors()
    {
        Dictionary<string, FragmentInfo> frags = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < Prefixes.Length; i++)
        {
            AddOrUpdateFragment(frags, PrefixesByOffset, Prefixes[i], PrefixRunLengths, 35, (e, p, o, r) => e with
            {
                IsPrefix = true,
                IsVowelPrefix = VowelPrefixes.Contains(p),
                PrefixIndex = i,
                PrefixOffset = o,
                PrefixRunLength = r
            });
        }

        for (int i = 0; i < VowelInfixes.Length; i++)
        {
            AddOrUpdateFragment(frags, VowelInfixesByOffset, VowelInfixes[i], InfixRunLengths, NonVowelSuffixes.Length, (e, p, o, r) => e with
            {
                IsInfix = true,
                IsVowelInfix = true,
                InfixIndex = i,
                InfixOffset = o,
                InfixRunLength = r
            });
        }

        for (int i = 0; i < NonVowelInfixes.Length; i++)
        {
            AddOrUpdateFragment(frags, NonVowelInfixesByOffset, NonVowelInfixes[i], InfixRunLengths, VowelSuffixes.Length, (e, p, o, r) => e with
            {
                IsInfix = true,
                IsVowelInfix = false,
                InfixIndex = i,
                InfixOffset = o,
                InfixRunLength = r
            });
        }

        for (int i = 0; i < VowelSuffixes.Length; i++)
        {
            AddOrUpdateFragment(frags, VowelSuffixes[i], (e, p) => e with
            {
                IsSuffix = true,
                IsVowelSuffix = true,
                SuffixIndex = i
            });
        }

        for (int i = 0; i < NonVowelSuffixes.Length; i++)
        {
            AddOrUpdateFragment(frags, NonVowelSuffixes[i], (e, p) => e with
            {
                IsSuffix = true,
                IsVowelSuffix = false,
                SuffixIndex = i
            });
        }

        Fragments = [.. frags.Values.OrderByDescending(f => f.Value.Length).ThenBy(f => f.Value)];
    }

    // Sector coords to sector name - based on https://bitbucket.org/Esvandiary/edts/src/develop/pgnames.py
    public static string GetSectorName(ByteXYZ pos)
    {
        if (!pos.IsValid)
        {
            throw new ArgumentException("Invalid sector position", nameof(pos));
        }

        if (CachedSectorsByCoords.TryGetValue(pos, out string? value))
        {
            return value;
        }

        int offset = pos.Ord;
        return CachedSectorsByCoords[pos] = IsC1Sector(offset) ? GetC1Name(offset) : GetC2Name(offset);
    }

    public static string GetSectorName(int sectorid)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sectorid, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sectorid, MaxSectorId);

        var pos = ByteXYZ.FromSectorId(sectorid);
        return GetSectorName(pos);
    }

    public static string GetC1SectorName(ByteXYZ pos)
    {
        return GetC1Name(pos.Ord);
    }

    public static string GetC2SectorName(ByteXYZ pos, bool test = false)
    {
        return GetC2Name(pos.Ord, test);
    }

    private static bool IsC1Sector(int offset)
    {
        unchecked
        {
            uint key = (uint)offset;

            // 32-bit hashing algorithm found at http://papa.bretmulvey.com/post/124027987928/hash-functions
            // Seemingly originally by Bob Jenkins <bob_jenkins-at-burtleburtle.net> in the 1990s
            key += key << 12;
            key ^= key >> 22;
            key += key << 4;
            key ^= key >> 9;
            key += key << 10;
            key ^= key >> 2;
            key += key << 7;
            key ^= key >> 12;

            return (key & 1) == 0;
        }
    }

    private static string ExtractC1Prefix(int offset, out int nextOffset, out bool isVowel)
    {
        int offsetNumerator = Math.DivRem(offset, PrefixesByOffset.Count, out int prefixOffset);
        var (prefix, ofs, runlen) = PrefixesByOffset[prefixOffset];
        nextOffset = offsetNumerator * runlen + prefixOffset - ofs;
        isVowel = VowelPrefixes.Contains(prefix);
        return prefix;
    }

    private static string ExtractC1Infix(int offset, bool isVowel, out int nextOffset)
    {
        var infixes = isVowel ? VowelInfixesByOffset : NonVowelInfixesByOffset;
        int offsetNumerator = Math.DivRem(offset, infixes.Count, out int infixOffset);
        var (infix, start, runlen) = infixes[infixOffset];
        nextOffset = offsetNumerator * runlen + infixOffset - start;
        return infix;
    }

    private static string GetC1Name(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, MaxOrd);

        List<string> frags = [];

        frags.Add(ExtractC1Prefix(offset, out offset, out bool prefixIsVowel));
        frags.Add(ExtractC1Infix(offset, !prefixIsVowel, out offset));

        var suffixes = prefixIsVowel ? VowelSuffixes : NonVowelSuffixes;

        if (offset >= suffixes.Length)
        {
            frags.Add(ExtractC1Infix(offset, prefixIsVowel, out offset));
            suffixes = !prefixIsVowel ? VowelSuffixes : NonVowelSuffixes;
        }

        // This is theoretical as there are no systems where there would be a third infix
        if (offset >= suffixes.Length)
        {
            frags.Add(ExtractC1Infix(offset, !prefixIsVowel, out offset));
            suffixes = prefixIsVowel ? VowelSuffixes : NonVowelSuffixes;
        }

        if (offset >= suffixes.Length)
        {
            throw new NotSupportedException("Bad C1 name offset");
        }

        frags.Add(suffixes[offset]);
        return string.Concat(frags);
    }

    private static string GetC2Name(int offset, bool test = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, MaxOrd);

        var (offset1, offset2) = Deinterleave2((uint)offset);
        var (prefix1, start1, _) = PrefixesByOffset[offset1];
        var (prefix2, start2, _) = PrefixesByOffset[offset2];

        string[] suffixes1 = VowelPrefixes.Contains(prefix1) ? NonVowelSuffixes : VowelSuffixes;
        string[] suffixes2 = VowelPrefixes.Contains(prefix2) ? NonVowelSuffixes : VowelSuffixes;
        var suffix1Offset = offset1 - start1;
        var suffix2Offset = offset2 - start2;

        if (suffix1Offset < 0 || suffix1Offset >= suffixes1.Length)
        {
            throw new NotSupportedException("Bad C2 name 1 offset");
        }

        if (suffix2Offset < 0 || suffix2Offset >= suffixes2.Length)
        {
            throw new NotSupportedException("Bad C2 name 2 offset");
        }

        string suffix1 = suffixes1[suffix1Offset];
        string suffix2 = suffixes2[suffix2Offset];

        if (test)
        {
            var prefix1Frag = FindFragment(prefix1, true);
            var prefix2Frag = FindFragment(prefix2, true);
            var suffix1Frag = FindFragment(suffix1);
            var suffix2Frag = FindFragment(suffix2);

            if (!string.Equals(prefix1Frag.Value, prefix1, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Bad C2 name prefix 1 offset");
            }

            if (!string.Equals(prefix2Frag.Value, prefix2, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Bad C2 name prefix 2 offset");
            }

            if (!string.Equals(suffix1Frag.Value, suffix1, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Bad C2 name suffix 1 offset");
            }

            if (!string.Equals(suffix2Frag.Value, suffix2, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Bad C2 name suffix 2 offset");
            }

            if (suffix1Frag.IsVowelSuffix == prefix1Frag.IsVowelPrefix)
            {
                throw new NotSupportedException("Bad C2 name 1");
            }

            if (suffix2Frag.IsVowelSuffix == prefix2Frag.IsVowelPrefix)
            {
                throw new NotSupportedException("Bad C2 name 2");
            }
        }

        return $"{prefix1}{suffix1} {prefix2}{suffix2}";
    }

    private static FragmentInfo FindFragment(ReadOnlySpan<char> current, bool isPrefix = false)
    {
        foreach (var item in Fragments)
        {
            if (current.StartsWith(item.Value, StringComparison.OrdinalIgnoreCase) && (!isPrefix || item.IsPrefix))
            {
                return item;
            }
        }

        return default;
    }

    private static List<FragmentInfo>? GetSectorFragments(string name)
    {
        name = name.ToLowerInvariant();
        List<FragmentInfo> fragments = [];
        ReadOnlySpan<char> current = name;
        bool isPrefix = true;

        while (!current.IsEmpty)
        {
            isPrefix |= current.StartsWith(" ");
            current = current.Trim();

            FragmentInfo frag = FindFragment(current, isPrefix);

            if (frag.Value == null)
            {
                return null;
            }

            if (isPrefix)
            {
                frag = frag with
                {
                    IsSuffix = false,
                    IsInfix = false
                };
            }
            else if (frag.IsInfix && fragments.Count > 0 && frag.IsVowelInfix == !fragments[^1].IsVowelInfix)
            {
                frag = frag with { IsPrefix = false };
            }

            fragments.Add(frag);
            current = current[frag.Value.Length..];
            isPrefix = false;
        }

        return fragments;
    }

    public static ByteXYZ GetSectorPos(string name)
    {
        if (CachedSectorsByName.TryGetValue(name, out ByteXYZ value))
        {
            return value;
        }

        return GetSectorFragments(name) switch
        {
            null => ByteXYZ.Invalid,
            [{ IsPrefix: true } p1, { IsSuffix: true } s1, { IsPrefix: true } p2, { IsSuffix: true } s2]
                => CachedSectorsByName[name] = GetC2SectorPos(p1, s1, p2, s2),
            [{ IsPrefix: true } p, { IsInfix: true } i, { IsSuffix: true } s]
                => CachedSectorsByName[name] = GetC1SectorPos3(p, i, s),
            [{ IsPrefix: true } p, { IsInfix: true } i1, { IsInfix: true } i2, { IsSuffix: true } s]
                => CachedSectorsByName[name] = GetC1SectorPos4(p, i1, i2, s),
            // This is theoretical, as there are no systems where there would be a third infix
            [{ IsPrefix: true } p, { IsInfix: true } i1, { IsInfix: true } i2, { IsInfix: true } i3, { IsSuffix: true } s]
                => CachedSectorsByName[name] = GetC1SectorPos5(p, i1, i2, i3, s),
            _ => ByteXYZ.Invalid
        };
    }

    public static bool TryGetSectorId(string name, out int sectorid)
    {
        var pos = GetSectorPos(name);

        if (!pos.IsValid)
        {
            sectorid = default;
            return false;
        }
        else
        {
            sectorid = pos.SectorId;
            return true;
        }
    }

    private static ByteXYZ GetC2SectorPos(FragmentInfo prefix1, FragmentInfo suffix1, FragmentInfo prefix2, FragmentInfo suffix2)
    {
        if (prefix1.IsVowelPrefix == suffix1.IsVowelSuffix || prefix2.IsVowelPrefix == suffix2.IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int idx0 = prefix1.PrefixOffset + suffix1.SuffixIndex;
        int idx1 = prefix2.PrefixOffset + suffix2.SuffixIndex;
        uint offset = Interleave2((ushort)idx0, (ushort)idx1);
        return ByteXYZ.FromOrdinal(offset);
    }

    private static int C1ProcessInfixFragment(FragmentInfo frag, int offset)
        => Math.DivRem(offset, InfixRunLengths[frag.Value], out int infixOffset)
         * (frag.IsVowelInfix ? VowelInfixesByOffset.Count : NonVowelInfixesByOffset.Count)
         + infixOffset
         + frag.InfixOffset;

    private static int C1ProcessPrefixFragment(FragmentInfo frag, int offset)
        => Math.DivRem(offset, PrefixRunLengths[frag.Value], out int prefixOffset)
         * PrefixesByOffset.Count
         + prefixOffset
         + frag.PrefixOffset;

    private static ByteXYZ GetC1SectorPos4(FragmentInfo prefix, FragmentInfo infix1, FragmentInfo infix2, FragmentInfo suffix)
    {
        if (prefix.IsVowelPrefix == infix1.IsVowelInfix
            || infix1.IsVowelInfix == infix2.IsVowelInfix
            || infix2.IsVowelInfix == suffix.IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int offset = suffix.SuffixIndex;
        offset = C1ProcessInfixFragment(infix2, offset);
        offset = C1ProcessInfixFragment(infix1, offset);
        offset = C1ProcessPrefixFragment(prefix, offset);

        if (offset > MaxOrd)
        {
            return ByteXYZ.Invalid;
        }

        return ByteXYZ.FromOrdinal(offset);
    }

    // This is theoretical, as there are no systems where there would be a third infix
    private static ByteXYZ GetC1SectorPos5(FragmentInfo prefix, FragmentInfo infix1, FragmentInfo infix2, FragmentInfo infix3, FragmentInfo suffix)
    {
        if (prefix.IsVowelPrefix == infix1.IsVowelInfix
            || infix1.IsVowelInfix == infix2.IsVowelInfix
            || infix2.IsVowelInfix == infix3.IsVowelInfix
            || infix3.IsVowelInfix == suffix.IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int offset = suffix.SuffixIndex;
        offset = C1ProcessInfixFragment(infix3, offset);
        offset = C1ProcessInfixFragment(infix2, offset);
        offset = C1ProcessInfixFragment(infix1, offset);
        offset = C1ProcessPrefixFragment(prefix, offset);
        return ByteXYZ.FromOrdinal(offset);
    }

    private static ByteXYZ GetC1SectorPos3(FragmentInfo prefix, FragmentInfo infix, FragmentInfo suffix)
    {
        if (prefix.IsVowelPrefix == infix.IsVowelInfix || infix.IsVowelInfix == suffix.IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int offset = suffix.SuffixIndex;
        offset = C1ProcessInfixFragment(infix, offset);
        offset = C1ProcessPrefixFragment(prefix, offset);
        return ByteXYZ.FromOrdinal(offset);
    }

    private static uint Interleave2(ushort v1, ushort v2)
    {
        unchecked
        {
            // Interleave two 16-bit values into a 32-bit Morton code (Z-order curve).

            if (Bmi2.IsSupported)
            {
                return Bmi2.ParallelBitDeposit(v1, _mask1x2u16)
                     | Bmi2.ParallelBitDeposit(v2, _mask1x2u16 << 1);
            }
            else if (Vector64<uint>.IsSupported)
            {
                var x = Vector64.Create((uint)v1, (uint)v2);

                x = (x | x << 8) & Vector64.Create(_mask8x2u16);
                x = (x | x << 4) & Vector64.Create(_mask4x2u16);
                x = (x | x << 2) & Vector64.Create(_mask2x2u16);
                x = (x | x << 1) & Vector64.Create(_mask1x2u16);

                return x[0] | (x[1] << 1);
            }
            else
            {
                var (x1, x2) = ((uint)v1, (uint)v2);

                (x1, x2) = ((x1 | (x1 << 8)) & _mask8x2u16, (x2 | (x2 << 8)) & _mask8x2u16);
                (x1, x2) = ((x1 | (x1 << 4)) & _mask4x2u16, (x2 | (x2 << 4)) & _mask4x2u16);
                (x1, x2) = ((x1 | (x1 << 2)) & _mask2x2u16, (x2 | (x2 << 2)) & _mask2x2u16);
                (x1, x2) = ((x1 | (x1 << 1)) & _mask1x2u16, (x2 | (x2 << 1)) & _mask1x2u16);

                return x1 | (x2 << 1);
            }
        }
    }

    private static (ushort v1, ushort v2) Deinterleave2(uint val)
    {
        unchecked
        {
            // Reverse of Interleave2 (Morton-style 2-way bit interleave)

            if (Bmi2.IsSupported)
            {
                return (
                    (ushort)Bmi2.ParallelBitExtract(val, _mask1x2u16),
                    (ushort)Bmi2.ParallelBitExtract(val, _mask1x2u16 << 1)
                );
            }
            else if (Vector64<uint>.IsSupported)
            {
                var x = Vector64.Create(val, val >> 1) & Vector64.Create(_mask1x2u16);

                x = (x | (x >> 1)) & Vector64.Create(_mask2x2u16);
                x = (x | (x >> 2)) & Vector64.Create(_mask4x2u16);
                x = (x | (x >> 4)) & Vector64.Create(_mask8x2u16);
                x = (x | (x >> 8)) & Vector64.Create(_mask16x2u16);

                return ((ushort)x[0], (ushort)x[1]);
            }
            else
            {
                var (x1, x2) = (val & _mask1x2u16, (val >> 1) & _mask1x2u16);

                (x1, x2) = ((x1 | (x1 >> 1)) & _mask2x2u16, (x2 | (x2 >> 1)) & _mask2x2u16);
                (x1, x2) = ((x1 | (x1 >> 1)) & _mask4x2u16, (x2 | (x2 >> 1)) & _mask4x2u16);
                (x1, x2) = ((x1 | (x1 >> 1)) & _mask8x2u16, (x2 | (x2 >> 1)) & _mask8x2u16);

                return ((ushort)((x1 | (x1 >> 1)) & _mask16x2u16), (ushort)((x2 | (x2 >> 1)) & _mask16x2u16));
            }
        }
    }

    private static uint Interleave3(ByteXYZ val)
    {
        unchecked
        {
            // Interleave 3 coordinates (Morton/Z-order)

            if (Bmi2.IsSupported)
            {
                return Bmi2.ParallelBitDeposit((uint)val.X, _mask1x3u8)
                     | Bmi2.ParallelBitDeposit((uint)val.Y, _mask1x3u8 << 1)
                     | Bmi2.ParallelBitDeposit((uint)val.Z, _mask1x3u8 << 2);
            }
            else if (Vector128<uint>.IsSupported)
            {
                var x = Vector128.Create((uint)val.X, (uint)val.Y, (uint)val.Z, 0);

                x = (x | (x << 8)) & Vector128.Create(_mask4x3u8);
                x = (x | (x << 4)) & Vector128.Create(_mask2x3u8);
                x = (x | (x << 2)) & Vector128.Create(_mask1x3u8);

                // Fold the three separated 21-bit lanes together to produce the final
                // 21-bit Morton code (bits ordered x0,y0,z0,x1,y1,z1,...).
                return x[0] | (x[1] << 1) | (x[2] << 2);
            }
            else
            {
                var (x, y, z) = ((uint)val.X, (uint)val.Y, (uint)val.Z);

                (x, y, z) = ((x | (x << 8)) & _mask4x3u8, (y | (y << 8)) & _mask4x3u8, (z | (z << 8)) & _mask4x3u8);
                (x, y, z) = ((x | (x << 4)) & _mask4x3u8, (y | (y << 4)) & _mask4x3u8, (z | (z << 4)) & _mask4x3u8);
                (x, y, z) = ((x | (x << 2)) & _mask4x3u8, (y | (y << 2)) & _mask4x3u8, (z | (z << 2)) & _mask4x3u8);

                return x | (y << 1) | (z << 2);
            }
        }
    }

    private static ByteXYZ Deinterleave3(uint val)
    {
        unchecked
        {
            if (Bmi2.IsSupported)
            {
                return new(
                    (sbyte)Bmi2.ParallelBitExtract(val, _mask1x3u8),
                    (sbyte)Bmi2.ParallelBitExtract(val, _mask1x3u8 << 1),
                    (sbyte)Bmi2.ParallelBitExtract(val, _mask1x3u8 << 2)
                );
            }
            else if (Vector128<uint>.IsSupported)
            {
                var x = Vector128.Create(val, val >> 1, val >> 2, 0) & Vector128.Create(_mask1x3u8);

                x = (x | (x >> 2)) & Vector128.Create(_mask2x3u8);
                x = (x | (x >> 4)) & Vector128.Create(_mask4x3u8);
                x = (x | (x >> 8)) & Vector128.Create(_mask8x3u8);

                return new((sbyte)x[0], (sbyte)x[1], (sbyte)x[2]);
            }
            else
            {
                var (x, y, z) = (val, val >> 1, val >> 2);

                (x, y, z) = ((x | (x >> 2)) & _mask2x3u8, (y | (y >> 2)) & _mask2x3u8, (z | (z >> 2)) & _mask2x3u8);
                (x, y, z) = ((x | (x >> 4)) & _mask4x3u8, (y | (y >> 4)) & _mask4x3u8, (z | (z >> 4)) & _mask4x3u8);
                (x, y, z) = ((x | (x >> 8)) & _mask8x3u8, (y | (y >> 8)) & _mask8x3u8, (z | (z >> 8)) & _mask8x3u8);

                return new((sbyte)x, (sbyte)y, (sbyte)z);
            }
        }
    }
}
