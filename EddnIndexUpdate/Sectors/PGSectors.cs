using EddnIndexUpdate.Models;

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

    [System.Diagnostics.DebuggerDisplay("({X},{Y},{Z})")]
    public readonly struct ByteXYZ(sbyte x, sbyte y, sbyte z) : IComparable<ByteXYZ>
    {
        public readonly sbyte X = x;
        public readonly sbyte Y = y;
        public readonly sbyte Z = z;

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
            return Ord.CompareTo(other.Ord);
        }

        public override readonly bool Equals(object? obj)
        {
            return obj != null && obj is ByteXYZ xyz && Ord.Equals(xyz.Ord);
        }

        public override int GetHashCode()
        {
            return Ord.GetHashCode();
        }

        public static bool operator ==(ByteXYZ left, ByteXYZ right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ByteXYZ left, ByteXYZ right)
        {
            return !left.Equals(right);
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
            bool IsC1VowelPrefix = false,
            bool IsC2VowelPrefix = false,
            int PrefixIndex = 0,
            bool IsInfix = false,
            bool IsVowelInfix = false,
            int InfixIndex = 0,
            bool IsSuffix = false,
            bool IsVowelSuffix = false,
            int SuffixIndex = 0
    );

    // Tables of prefixes, infixes and suffixes from https://bitbucket.org/Esvandiary/edts/src/master/pgdata.py
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

    // Vowelish C2 prefixes
    private static readonly HashSet<string> C2VowelPrefixes = new(
    [
        "Eo",  "Oo",  "Eu",  "Ou",  "Ae",  "Ai",  "Eae", "Ao",
        "Au",  "Aae"
    ], StringComparer.OrdinalIgnoreCase);

    // Vowelish C1 prefixes
    private static readonly HashSet<string> C1VowelPrefixes = new(
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

    private static readonly FragmentInfo[] Fragments = FillFragments(Prefixes, VowelInfixes, NonVowelInfixes, VowelSuffixes, NonVowelSuffixes);

    private static readonly Dictionary<string, int> PrefixOffsets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly int PrefixTotalRunLength = FillOffsets(Prefixes, PrefixRunLengths, PrefixOffsets, 35);

    private static readonly Dictionary<string, int> InfixOffsets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly int VowelInfixesTotalRunLength = FillOffsets(VowelInfixes, InfixRunLengths, InfixOffsets, NonVowelSuffixes.Length);
    private static readonly int NonVowelInfixesTotalRunLength = FillOffsets(NonVowelInfixes, InfixRunLengths, InfixOffsets, VowelSuffixes.Length);

    private static readonly Dictionary<ByteXYZ, string> CachedSectorsByCoords = [];
    private static readonly Dictionary<string, ByteXYZ> CachedSectorsByName = new(StringComparer.OrdinalIgnoreCase);

    private static int FillOffsets(string[] prefixes, Dictionary<string, int> runlengths, Dictionary<string, int> offsets, int defaultlen)
    {
        int cnt = 0;
        foreach (string p in prefixes)
        {
            if (!runlengths.TryGetValue(p, out int plen))
            {
                plen = defaultlen;
                runlengths[p] = plen;
            }

            offsets[p] = cnt;
            cnt += plen;
        }

        return cnt;
    }

    private static void AddOrUpdateFragment(Dictionary<string, FragmentInfo> frags, string value, Func<FragmentInfo, string, FragmentInfo> modifyAction)
    {
        string p = value.ToLowerInvariant();

        var frag = frags.TryGetValue(p, out FragmentInfo v)
                 ? v
                 : new FragmentInfo { Value = p };

        frags[p] = modifyAction(frag, p);
    }

    private static FragmentInfo[] FillFragments(string[] prefixes, string[] vowelInfixes, string[] nonVowelInfixes, string[] vowelSuffixes, string[] nonVowelSuffixes)
    {
        Dictionary<string, FragmentInfo> frags = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < prefixes.Length; i++)
        {
            AddOrUpdateFragment(frags, prefixes[i], (e, p) => e with
            {
                IsPrefix = true,
                IsC1VowelPrefix = C1VowelPrefixes.Contains(p),
                IsC2VowelPrefix = C2VowelPrefixes.Contains(p),
                PrefixIndex = i
            });
        }

        for (int i = 0; i < vowelInfixes.Length; i++)
        {
            AddOrUpdateFragment(frags, vowelInfixes[i], (e, _) => e with
            {
                IsInfix = true,
                IsVowelInfix = true,
                InfixIndex = i
            });
        }

        for (int i = 0; i < nonVowelInfixes.Length; i++)
        {
            AddOrUpdateFragment(frags, nonVowelInfixes[i], (e, _) => e with
            {
                IsInfix = true,
                IsVowelInfix = false,
                InfixIndex = i
            });
        }

        for (int i = 0; i < vowelSuffixes.Length; i++)
        {
            AddOrUpdateFragment(frags, vowelSuffixes[i], (e, _) => e with
            {
                IsSuffix = true,
                IsVowelSuffix = true,
                SuffixIndex = i
            });
        }

        for (int i = 0; i < nonVowelSuffixes.Length; i++)
        {
            AddOrUpdateFragment(frags, nonVowelSuffixes[i], (e, _) => e with
            {
                IsSuffix = true,
                IsVowelSuffix = false,
                SuffixIndex = i
            });
        }

        return [.. frags.Values.OrderByDescending(f => f.Value.Length).ThenBy(f => f.Value)];
    }

    // Sector coords to sector name - based on https://bitbucket.org/Esvandiary/edts/src/master/pgnames.py
    public static string GetSectorName(ByteXYZ pos)
    {
        if (CachedSectorsByCoords.TryGetValue(pos, out string? value))
        {
            return value;
        }

        int offset = pos.Ord;
        return CachedSectorsByCoords[pos] = IsC1Sector(offset) ? GetC1Name(offset) : GetC2Name(offset);
    }

    public static string GetSectorName(int sectorid)
    {
        var pos = ByteXYZ.FromSectorId(sectorid);
        return GetSectorName(pos);
    }

    public static string GetC1SectorName(ByteXYZ pos)
    {
        return GetC1Name(pos.Ord);
    }

    public static string GetC2SectorName(ByteXYZ pos)
    {
        return GetC2Name(pos.Ord);
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

    private static string ExtractC1Prefix(int offset, out int next_idx, out bool isVowel)
    {
        int prefix_cnt = Math.DivRem(offset, PrefixTotalRunLength, out int rem);
        string prefix = Prefixes.Last(p => PrefixOffsets[p] <= rem);
        next_idx = prefix_cnt * PrefixRunLengths[prefix] + rem - PrefixOffsets[prefix];
        isVowel = C1VowelPrefixes.Contains(prefix);
        return prefix;
    }

    private static string ExtractC1Infix(int offset, bool isVowel, out int next_idx)
    {
        int infix_total_len = isVowel ? VowelInfixesTotalRunLength : NonVowelInfixesTotalRunLength;
        string[] infixes = isVowel ? NonVowelInfixes : VowelInfixes;
        int infix_cnt = Math.DivRem(offset, infix_total_len, out int cur_offset);
        string infix = infixes.Last(p => InfixOffsets[p] <= cur_offset);
        cur_offset -= InfixOffsets[infix];
        int infix1_run_len = InfixRunLengths[infix];
        next_idx = infix1_run_len * infix_cnt + cur_offset;
        return infix;
    }

    private static string GetC1Name(int offset)
    {
        List<string> frags = [];

        frags.Add(ExtractC1Prefix(offset, out offset, out bool prefixIsVowel));
        frags.Add(ExtractC1Infix(offset, !prefixIsVowel, out offset));

        var suffixes = prefixIsVowel ? VowelSuffixes : NonVowelSuffixes;

        if (offset >= suffixes.Length)
        {
            frags.Add(ExtractC1Infix(offset, prefixIsVowel, out offset));
            suffixes = !prefixIsVowel ? VowelSuffixes : NonVowelSuffixes;
        }

        if (offset >= suffixes.Length)
        {
            throw new InvalidOperationException("Bad C1 name offset");
        }

        frags.Add(suffixes[offset]);
        return string.Concat(frags);
    }

    private static string GetC2Name(int offset)
    {
        Tuple<ushort, ushort> cur_idx = Deinterleave2((uint)offset);
        string p1 = Prefixes.Last(p => PrefixOffsets[p] <= cur_idx.Item1);
        string p2 = Prefixes.Last(p => PrefixOffsets[p] <= cur_idx.Item2);
        string[] s1s = C2VowelPrefixes.Contains(p1) ? NonVowelSuffixes : VowelSuffixes;
        string[] s2s = C2VowelPrefixes.Contains(p2) ? NonVowelSuffixes : VowelSuffixes;
        string s1 = s1s[cur_idx.Item1 - PrefixOffsets[p1]];
        string s2 = s2s[cur_idx.Item2 - PrefixOffsets[p2]];
        return $"{p1}{s1} {p2}{s2}";
    }

    private static FragmentInfo FindFragment(ReadOnlySpan<char> current)
    {
        foreach (var item in Fragments)
        {
            if (current.StartsWith(item.Value))
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

        while (!current.IsEmpty)
        {
            bool spacestart = current.StartsWith(" ");
            current = current.Trim();

            FragmentInfo frag = FindFragment(current);

            if (frag.Value == null)
            {
                return null;
            }

            if (spacestart)
            {
                frag = frag with
                {
                    IsSuffix = false,
                    IsInfix = false
                };
            }
            else if (fragments.Count != 0 && frag.IsInfix && frag.IsVowelInfix != fragments[^1].IsVowelInfix)
            {
                frag = frag with { IsPrefix = false };
            }

            fragments.Add(frag);
            current = current[frag.Value.Length..];
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
            _ => ByteXYZ.Invalid
        };
    }

    public static bool TryGetSectorId(string name, out int sectorid)
    {
        var pos = GetSectorPos(name);

        if (pos.X < 0 || pos.Y < 0 || pos.Z < 0)
        {
            sectorid = 0;
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
        if (prefix1.IsC2VowelPrefix == suffix1.IsVowelSuffix || prefix2.IsC2VowelPrefix == suffix2.IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int idx0 = PrefixOffsets[prefix1.Value] + suffix1.SuffixIndex;
        int idx1 = PrefixOffsets[prefix2.Value] + suffix2.SuffixIndex;
        uint offset = Interleave2((ushort)idx0, (ushort)idx1);
        return ByteXYZ.FromOrdinal(offset);
    }

    private static int InfixTotalRunLength(FragmentInfo frag)
        => frag.IsVowelInfix ? VowelInfixesTotalRunLength : NonVowelInfixesTotalRunLength;

    private static int C1ProcessInfixFragment(FragmentInfo frag, int offset)
        => Math.DivRem(offset, InfixRunLengths[frag.Value], out int offset_mod)
         * InfixTotalRunLength(frag)
         + offset_mod
         + InfixOffsets[frag.Value];

    private static int C1ProcessPrefixFragment(FragmentInfo frag, int offset)
        => Math.DivRem(offset, PrefixRunLengths[frag.Value], out int offset_mod)
         * PrefixTotalRunLength
         + offset_mod
         + PrefixOffsets[frag.Value];

    private static int C1ProcessSuffix4Fragment(FragmentInfo suffix, FragmentInfo infix2)
        => suffix.SuffixIndex
         + suffix.SuffixIndex / InfixRunLengths[infix2.Value] * InfixTotalRunLength(infix2);

    private static ByteXYZ GetC1SectorPos4(FragmentInfo prefix, FragmentInfo infix1, FragmentInfo infix2, FragmentInfo suffix)
    {
        if (prefix.IsC1VowelPrefix == infix1.IsVowelInfix
            || infix1.IsVowelInfix == infix2.IsVowelInfix
            || infix2.IsVowelInfix == suffix.IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int offset = C1ProcessSuffix4Fragment(suffix, infix2);
        offset = C1ProcessInfixFragment(infix2, offset);
        offset = C1ProcessInfixFragment(infix1, offset);
        offset = C1ProcessPrefixFragment(prefix, offset);
        return ByteXYZ.FromOrdinal(offset);
    }

    private static ByteXYZ GetC1SectorPos3(FragmentInfo prefix, FragmentInfo infix, FragmentInfo suffix)
    {
        if (prefix.IsC1VowelPrefix == infix.IsVowelInfix || infix.IsVowelInfix == suffix.IsVowelSuffix)
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
            // Layout strategy:
            // - place v1 in low 16 bits
            // - place v2 in bits 32..47
            // Then progressively "spread" bits so each original bit is separated by one zero bit.
            ulong x = v1 | (ulong)v2 << 32;

            // Spread to byte granularity: keep only alternating 8-bit groups.
            x = (x | x << 8) & 0x00FF00FF00FF00FFUL;

            // Spread to nibble granularity (4-bit groups).
            x = (x | x << 4) & 0x0F0F0F0F0F0F0F0FUL;

            // Spread to 2-bit groups.
            x = (x | x << 2) & 0x3333333333333333UL;

            // Spread to single-bit spacing: retain bits in even positions only.
            x = (x | x << 1) & 0x5555555555555555UL;

            // Merge the two spread lanes:
            // - low lane (from v1) already occupies even bit positions
            // - high lane (from v2) is shifted down by 31 so it occupies odd positions.
            return (uint)((x | x >> 31) & 0xFFFFFFFF);
        }
    }

    private static Tuple<ushort, ushort> Deinterleave2(uint val)
    {
        unchecked
        {
            // Reverse of Interleave2 (Morton-style 2-way bit interleave):
            // - even-position bits in val belong to v1
            // - odd-position bits in val belong to v2
            // Place even bits in the low 32-bit lane and odd bits in the high lane.
            // 0x55555555 = 0101... selects even bits, 0xAAAAAAAA = 1010... selects odd bits.
            ulong x = val & 0x55555555UL | (val & 0xAAAAAAAAUL) << 31;

            // Progressively "compact" separated bits in each lane back into contiguous values.
            // Each stage merges neighboring groups and masks away interleaving gaps.
            x = (x | x >> 1) & 0x3333333333333333UL;
            x = (x | x >> 2) & 0x0F0F0F0F0F0F0F0FUL;
            x = (x | x >> 4) & 0x00FF00FF00FF00FFUL;
            x = (x | x >> 8) & 0x0000FFFF0000FFFFUL;

            // Extract original 16-bit values:
            // - low 16 bits  => first input (v1)
            // - bits 32..47 => second input (v2)
            return new Tuple<ushort, ushort>((ushort)(x & 0xFFFF), (ushort)(x >> 32 & 0xFFFF));
        }
    }

    private static uint Interleave3(ByteXYZ val)
    {
        unchecked
        {
            // Interleave 3 coordinates (Morton/Z-order), using 7 bits from each axis.
            // Initial packing layout in x: [Z6..Z0][Y6..Y0][X6..X0] (21 bits total).
            ulong x = (ulong)val.Ord;

            // Spread the packed bits apart in stages; each mask keeps only lanes that
            // can still contribute to the final every-3rd-bit pattern.
            x = (x | x << 32) & 0x001F00000000FFFFUL;
            x = (x | x << 16) & 0x001F0000FF0000FFUL;
            x = (x | x << 8) & 0x100F00F00F00F00FUL;
            x = (x | x << 4) & 0x10C30C30C30C30C3UL;
            x = (x | x << 2) & 0x1249249249249249UL;

            // Fold the three separated 21-bit lanes together to produce the final
            // 21-bit Morton code (bits ordered x0,y0,z0,x1,y1,z1,...).
            return (uint)((x | x >> 20 | x >> 40) & 0x1FFFFF);
        }
    }

    private static ByteXYZ Deinterleave3(uint val)
    {
        unchecked
        {
            ulong x =  (ulong)val & 0b001001001001001001001
                    | ((ulong)val & 0b010010010010010010010) << 20
                    | ((ulong)val & 0b100100100100100100100) << 40;

            x = (x | x >> 2) & 0x10C30C30C30C30C3UL;
            x = (x | x >> 4) & 0x100F00F00F00F00FUL;
            x = (x | x >> 8) & 0x001F0000FF0000FFUL;
            x = (x | x >> 16) & 0x001F00000000FFFFUL;
            x = (x | x >> 32) & 0x00000000001FFFFFUL;
            return ByteXYZ.FromOrdinal((uint)x);
        }
    }
}
