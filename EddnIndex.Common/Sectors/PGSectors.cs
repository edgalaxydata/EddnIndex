using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace EddnIndex.Common.Sectors;

public static class PGSectors
{
    private const int ORD_BITS = 7;
    private const int X_BITS = 7;
    private const int Y_BITS = 6;
    private const int Z_BITS = 7;
    private const int ORD_MASK = (1 << ORD_BITS) - 1;
    private const int X_MASK = (1 << X_BITS) - 1;
    private const int Y_MASK = (1 << Y_BITS) - 1;
    private const int Z_MASK = (1 << Z_BITS) - 1;
    private const int X_SHIFT = 0;
    private const int Y_SHIFT = X_SHIFT + X_BITS;
    private const int Z_SHIFT = Y_SHIFT + Y_BITS;
    private const int ORD_X_SHIFT = 0;
    private const int ORD_Y_SHIFT = ORD_X_SHIFT + ORD_BITS;
    private const int ORD_Z_SHIFT = ORD_Y_SHIFT + ORD_BITS;
    private const int ORD_X_STRIDE = 1;
    private const int ORD_Y_STRIDE = ORD_X_STRIDE << ORD_BITS;
    private const int ORD_Z_STRIDE = ORD_Y_STRIDE << ORD_BITS;
    private const int X_STRIDE = 1;
    private const int Y_STRIDE = X_STRIDE << X_BITS;
    private const int Z_STRIDE = Y_STRIDE << Y_BITS;
    private const int MAX_ORD = (1 << (ORD_BITS * 3)) - 1;
    private const int MAX_SECTOR_ID = (1 << (X_BITS + Y_BITS + Z_BITS)) - 1;

    private const uint MASK_8X3_U8 = 0b000_000_000_000_000_011_111_111U;
    private const uint MASK_4X3_U8 = 0b000_000_001_111_000_000_001_111U;
    private const uint MASK_2X3_U8 = 0b000_011_000_011_000_011_000_011U;
    private const uint MASK_1X3_U8 = 0b001_001_001_001_001_001_001_001U;

    private const uint MASK_16X2_U16 = 0b0000_0000_0000_0000_1111_1111_1111_1111U;
    private const uint MASK_8X2_U16  = 0b0000_0000_1111_1111_0000_0000_1111_1111U;
    private const uint MASK_4X2_U16  = 0b0000_1111_0000_1111_0000_1111_0000_1111U;
    private const uint MASK_2X2_U16  = 0b0011_0011_0011_0011_0011_0011_0011_0011U;
    private const uint MASK_1X2_U16  = 0b0101_0101_0101_0101_0101_0101_0101_0101U;

    public readonly record struct SectorCoord(sbyte X, sbyte Y, sbyte Z) : IComparable<SectorCoord>
    {
        public readonly bool IsValid => X is >= 0 and <= X_MASK
                                     && Y is >= 0 and <= Y_MASK
                                     && Z is >= 0 and <= Z_MASK;

        public readonly int Ord => IsValid
                                 ? (X * ORD_X_STRIDE) + (Y * ORD_Y_STRIDE) + (Z * ORD_Z_STRIDE)
                                 : -1;

        public readonly int SectorId => IsValid
                                      ? (X * X_STRIDE) + (Y * Y_STRIDE) + (Z * Z_STRIDE)
                                      : -1;

        public override readonly string ToString() => $"({X},{Y},{Z})";

        public readonly int CompareTo(SectorCoord other)
            => (X, Y, Z).CompareTo((other.X, other.Y, other.Z));

        public static readonly SectorCoord Invalid = new(sbyte.MinValue, sbyte.MinValue, sbyte.MinValue);

        public static SectorCoord FromSectorId(int sectorid)
            => new(
                (sbyte)((sectorid >> X_SHIFT) & X_MASK),
                (sbyte)((sectorid >> Y_SHIFT) & Y_MASK),
                (sbyte)((sectorid >> Z_SHIFT) & Z_MASK)
            );

        public static SectorCoord FromOrdinal(int ordinal)
            => new(
                (sbyte)((ordinal >> ORD_X_SHIFT) & ORD_MASK),
                (sbyte)((ordinal >> ORD_Y_SHIFT) & ORD_MASK),
                (sbyte)((ordinal >> ORD_Z_SHIFT) & ORD_MASK)
            );

        public static SectorCoord FromOrdinal(uint ordinal)
            => new(
                (sbyte)((ordinal >> ORD_X_SHIFT) & ORD_MASK),
                (sbyte)((ordinal >> ORD_Y_SHIFT) & ORD_MASK),
                (sbyte)((ordinal >> ORD_Z_SHIFT) & ORD_MASK)
            );
    }

    private readonly record struct FragmentInfo(
            string Value,
            bool IsVowelish = false,
            bool IsPrefix = false,
            int PrefixOffset = 0,
            int PrefixRunLength = 0,
            bool IsInfix = false,
            int InfixOffset = 0,
            int InfixRunLength = 0,
            bool IsSuffix = false,
            int SuffixIndex = 0
    );

    private class FragmentTrieNode
    {
        private const char MIN_VALUE = 'a';
        private const char MAX_VALUE = 'z';

        private FragmentInfo _value;
        private readonly Lock _lock = new();
        private volatile uint _usedNodeMask = 0;
        private volatile FragmentTrieNode[] _childNodes = [];

        private void Add(string fullKey, ReadOnlySpan<char> subKey, in FragmentInfo frag)
        {
            lock (_lock)
            {
                if (subKey.Length == 0)
                {
                    if (_value.Value != null)
                    {
                        throw new ArgumentException($"An item with the same key exists: {fullKey}", nameof(fullKey));
                    }

                    _value = frag;
                    return;
                }
            }

            char c0 = char.ToLowerInvariant(subKey[0]);

            ArgumentOutOfRangeException.ThrowIfLessThan(c0, MIN_VALUE, nameof(fullKey));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(c0, MAX_VALUE, nameof(fullKey));

            uint mask = 1U << (c0 - MIN_VALUE);
            FragmentTrieNode? newNode = null;

            while (true)
            {
                int pos;
                uint usedNodeMask;
                FragmentTrieNode[] childNodes;

                lock (_lock)
                {
                    usedNodeMask = _usedNodeMask;
                    childNodes = _childNodes;
                    pos = BitOperations.PopCount(_usedNodeMask & (mask - 1));
                }

                if ((usedNodeMask & mask) != 0)
                {
                    childNodes[pos].Add(fullKey, subKey[1..], in frag);
                    return;
                }

                newNode ??= new FragmentTrieNode();

                lock (_lock)
                {
                    if (!ReferenceEquals(childNodes, _childNodes) || usedNodeMask != _usedNodeMask)
                    {
                        continue;
                    }

                    _childNodes = [.. childNodes[..pos], newNode, .. childNodes[pos..]];
                    _usedNodeMask = usedNodeMask | mask;
                }

                newNode.Add(fullKey, subKey[1..], in frag);
                return;
            }
        }

        public void Add(in FragmentInfo frag)
            => Add(frag.Value, frag.Value, in frag);

        private bool TryFind(ReadOnlySpan<char> fullKey, ReadOnlySpan<char> subKey, out FragmentInfo frag)
        {
            lock (_lock)
            {
                if (subKey.Length == 0)
                {
                    frag = _value;
                    return frag.Value != null;
                }
            }

            char c0 = char.ToLowerInvariant(subKey[0]);

            if (c0 is < MIN_VALUE or > MAX_VALUE)
            {
                lock (_lock)
                {
                    frag = _value;
                    return frag.Value != null;
                }
            }

            uint mask = 1U << (c0 - MIN_VALUE);

            int pos;
            uint usedNodeMask;
            FragmentTrieNode[] childNodes;

            lock (_lock)
            {
                usedNodeMask = _usedNodeMask;
                childNodes = _childNodes;
                pos = BitOperations.PopCount(_usedNodeMask & (mask - 1));
            }

            if ((usedNodeMask & mask) != 0)
            {
                return childNodes[pos].TryFind(fullKey, subKey[1..], out frag);
            }

            frag = _value;
            return _value.Value != null;
        }

        public bool TryFind(ReadOnlySpan<char> key, out FragmentInfo frag)
            => TryFind(key, key, out frag);
    }

    // Tables of prefixes, infixes and suffixes from https://bitbucket.org/Esvandiary/edts/src/develop/pgdata.py
    // Prefixes
    private static readonly ImmutableArray<string> _prefixes =
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
    private static readonly ImmutableArray<string> _vowelInfixes =
    [
        "o",   "ai",  "a",   "oi",  "ea",  "ie",  "u",   "e",
        "ee",  "oo",  "ue",  "i",   "oa",  "au",  "ae",  "oe"
    ];

    // Consonantish infixes
    private static readonly ImmutableArray<string> _nonVowelInfixes =
    [
        "ll",  "ss",  "b",   "c",   "d",   "f",   "dg",  "g",
        "ng",  "h",   "j",   "k",   "l",   "m",   "n",   "mb",
        "p",   "q",   "gn",  "th",  "r",   "s",   "t",   "ch",
        "tch", "v",   "w",   "wh",  "ck",  "x",   "y",   "z",
        "ph",  "sh",  "ct",  "wr"
    ];

    // Vowelish suffixes
    private static readonly ImmutableArray<string> _vowelSuffixes =
    [
        "oe",  "io",  "oea", "oi",  "aa",  "ua", "eia", "ae",
        "ooe", "oo",  "a",   "ue",  "ai",  "e",  "iae", "oae",
        "ou",  "uae", "i",   "ao",  "au",  "o",  "eae", "u",
        "aea", "ia",  "ie",  "eou", "aei", "ea", "uia", "oa",
        "aae", "eau", "ee"
    ];

    // Consonantish suffixes
    private static readonly ImmutableArray<string> _nonVowelSuffixes =
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
    private static readonly FrozenSet<string> _vowelPrefixes =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
        [
            "Eo",  "Oo",  "Eu",  "Ou",  "Ae",  "Ai",  "Eae", "Ao",
            "Au",  "Aae", "A",   "Io",  "E",   "I",   "O",   "Ea",
            "U",   "Ee",  "Ei",  "Oe"
        ]);

    // Prefixes using short run lengths
    private static readonly Dictionary<string, int> _prefixRunLengths = new(StringComparer.OrdinalIgnoreCase)
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
    private static readonly Dictionary<string, int> _infixRunLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        // Sequence 1
        { "oi",   88 }, { "ue",  147 }, { "oa",   57 },
        { "au",  119 }, { "ae",   12 }, { "oe",   39 },
        // Sequence 2
        { "dg",   31 }, { "tch",  20 }, { "wr",   31 },
    };

    private static readonly FragmentTrieNode _fragmentTrie = new();

    private static readonly List<(string Value, int Offset, int RunLength)> _prefixesByOffset = [];

    private static readonly List<(string Value, int Offset, int RunLength)> _vowelInfixesByOffset = [];
    private static readonly List<(string Value, int Offset, int RunLength)> _nonVowelInfixesByOffset = [];

    private static readonly ConcurrentDictionary<SectorCoord, string> _cachedSectorsByCoords = [];
    private static readonly ConcurrentDictionary<string, SectorCoord> _cachedSectorsByName = [];

    private static void AddOrUpdateFragment(
            Dictionary<string, FragmentInfo> frags,
            List<(string Value, int Offset, int RunLength)> byOffset,
            string value,
            Dictionary<string, int> runlengths,
            int defaultlen,
            Func<FragmentInfo, string, int, int, FragmentInfo> modifyAction
        )
    {
        string valueLower = value.ToLowerInvariant();

        var frag = frags.TryGetValue(value, out var v)
                 ? v
                 : new FragmentInfo(value);

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

        var frag = frags.TryGetValue(value, out var v)
                 ? v
                 : new FragmentInfo(value);

        frags[value] = modifyAction(frag, value);
    }

    static PGSectors()
    {
        Dictionary<string, FragmentInfo> frags = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _prefixes.Length; i++)
        {
            AddOrUpdateFragment(frags, _prefixesByOffset, _prefixes[i], _prefixRunLengths, 35, (e, p, o, r) => e with
            {
                IsPrefix = true,
                IsVowelish = _vowelPrefixes.Contains(p),
                PrefixOffset = o,
                PrefixRunLength = r
            });
        }

        for (int i = 0; i < _vowelInfixes.Length; i++)
        {
            AddOrUpdateFragment(frags, _vowelInfixesByOffset, _vowelInfixes[i], _infixRunLengths, _nonVowelSuffixes.Length, (e, p, o, r) => e with
            {
                IsInfix = true,
                IsVowelish = true,
                InfixOffset = o,
                InfixRunLength = r
            });
        }

        for (int i = 0; i < _nonVowelInfixes.Length; i++)
        {
            AddOrUpdateFragment(frags, _nonVowelInfixesByOffset, _nonVowelInfixes[i], _infixRunLengths, _vowelSuffixes.Length, (e, p, o, r) => e with
            {
                IsInfix = true,
                IsVowelish = false,
                InfixOffset = o,
                InfixRunLength = r
            });
        }

        for (int i = 0; i < _vowelSuffixes.Length; i++)
        {
            AddOrUpdateFragment(frags, _vowelSuffixes[i], (e, p) => e with
            {
                IsSuffix = true,
                IsVowelish = true,
                SuffixIndex = i
            });
        }

        for (int i = 0; i < _nonVowelSuffixes.Length; i++)
        {
            AddOrUpdateFragment(frags, _nonVowelSuffixes[i], (e, p) => e with
            {
                IsSuffix = true,
                IsVowelish = false,
                SuffixIndex = i
            });
        }

        FragmentInfo[] fragments = [.. frags.Values.OrderByDescending(f => f.Value.Length).ThenBy(f => f.Value)];

        foreach (var frag in fragments)
        {
            _fragmentTrie.Add(in frag);
        }
    }

    // Sector coords to sector name - based on https://bitbucket.org/Esvandiary/edts/src/develop/pgnames.py
    public static string GetSectorName(SectorCoord pos)
    {
        if (!pos.IsValid)
        {
            throw new ArgumentException("Invalid sector position", nameof(pos));
        }

        return _cachedSectorsByCoords.GetOrAdd(pos, p => IsC1Sector(p.Ord) ? GetC1Name(p.Ord) : GetC2Name(p.Ord));
    }

    public static string GetSectorName(int sectorid)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sectorid, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sectorid, MAX_SECTOR_ID);

        var pos = SectorCoord.FromSectorId(sectorid);
        return GetSectorName(pos);
    }

    public static string GetC1SectorName(SectorCoord pos)
        => GetC1Name(pos.Ord);

    public static string GetC2SectorName(SectorCoord pos, bool test = false)
        => GetC2Name(pos.Ord, test);

    private static bool IsC1Sector(int offset)
    {
        unchecked
        {
            uint key = (uint)offset;

            // Source: Thomas Wang, "Integer Hash Function", section
            // "Robert Jenkins' 32 bit Mix Function" (Jan 1997; updated Jul 1999).
            // Archive: https://web.archive.org/web/19990903131503/http://www.concentric.net/~ttwang/tech/inthash.htm

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
        int offsetNumerator = Math.DivRem(offset, _prefixesByOffset.Count, out int prefixOffset);
        var (prefix, ofs, runlen) = _prefixesByOffset[prefixOffset];
        nextOffset = (offsetNumerator * runlen) + prefixOffset - ofs;
        isVowel = _vowelPrefixes.Contains(prefix);
        return prefix;
    }

    private static string ExtractC1Infix(int offset, bool isVowel, out int nextOffset)
    {
        var infixes = isVowel ? _vowelInfixesByOffset : _nonVowelInfixesByOffset;
        int offsetNumerator = Math.DivRem(offset, infixes.Count, out int infixOffset);
        var (infix, start, runlen) = infixes[infixOffset];
        nextOffset = (offsetNumerator * runlen) + infixOffset - start;
        return infix;
    }

    private static string GetC1Name(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(offset, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, MAX_ORD);

        List<string> frags = [];

        frags.Add(ExtractC1Prefix(offset, out offset, out bool prefixIsVowel));
        frags.Add(ExtractC1Infix(offset, !prefixIsVowel, out offset));

        var suffixes = prefixIsVowel ? _vowelSuffixes : _nonVowelSuffixes;

        if (offset >= suffixes.Length)
        {
            frags.Add(ExtractC1Infix(offset, prefixIsVowel, out offset));
            suffixes = !prefixIsVowel ? _vowelSuffixes : _nonVowelSuffixes;
        }

        // This is theoretical as there are no systems where there would be a third infix
        if (offset >= suffixes.Length)
        {
            frags.Add(ExtractC1Infix(offset, !prefixIsVowel, out offset));
            suffixes = prefixIsVowel ? _vowelSuffixes : _nonVowelSuffixes;
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
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, MAX_ORD);

        var (offset1, offset2) = Deinterleave2((uint)offset);

        if (offset1 >= _prefixesByOffset.Count)
        {
            throw new NotSupportedException("Bad C2 name 1 offset");
        }

        if (offset2 >= _prefixesByOffset.Count)
        {
            throw new NotSupportedException("Bad C2 name 2 offset");
        }

        var (prefix1, start1, _) = _prefixesByOffset[offset1];
        var (prefix2, start2, _) = _prefixesByOffset[offset2];

        var suffixes1 = _vowelPrefixes.Contains(prefix1) ? _nonVowelSuffixes : _vowelSuffixes;
        var suffixes2 = _vowelPrefixes.Contains(prefix2) ? _nonVowelSuffixes : _vowelSuffixes;
        int suffix1Offset = offset1 - start1;
        int suffix2Offset = offset2 - start2;

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
            var prefix1Frag = FindFragment(prefix1);
            var prefix2Frag = FindFragment(prefix2);
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

            if (suffix1Frag.IsVowelish == prefix1Frag.IsVowelish)
            {
                throw new NotSupportedException("Bad C2 name 1");
            }

            if (suffix2Frag.IsVowelish == prefix2Frag.IsVowelish)
            {
                throw new NotSupportedException("Bad C2 name 2");
            }
        }

        return $"{prefix1}{suffix1} {prefix2}{suffix2}";
    }

    private static FragmentInfo FindFragment(ReadOnlySpan<char> current)
    {
        if (_fragmentTrie.TryFind(current, out var frag))
        {
            return frag;
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

            var frag = FindFragment(current);

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
            else if (frag.IsInfix && fragments.Count > 0 && frag.IsVowelish == !fragments[^1].IsVowelish)
            {
                frag = frag with { IsPrefix = false };
            }

            fragments.Add(frag);
            current = current[frag.Value.Length..];
            isPrefix = false;
        }

        return fragments;
    }

    public static SectorCoord GetSectorPos(string name)
    {
        return _cachedSectorsByName.GetOrAdd(name.ToLowerInvariant(), n => GetSectorFragments(n) switch
        {
            null => SectorCoord.Invalid,
            [{ IsPrefix: true } p1, { IsSuffix: true } s1, { IsPrefix: true } p2, { IsSuffix: true } s2]
                => GetC2SectorPos(p1, s1, p2, s2),
            [{ IsPrefix: true } p, { IsInfix: true } i, { IsSuffix: true } s]
                => GetC1SectorPos3(p, i, s),
            [{ IsPrefix: true } p, { IsInfix: true } i1, { IsInfix: true } i2, { IsSuffix: true } s]
                => GetC1SectorPos4(p, i1, i2, s),
            // This is theoretical, as there are no systems where there would be a third infix
            [{ IsPrefix: true } p, { IsInfix: true } i1, { IsInfix: true } i2, { IsInfix: true } i3, { IsSuffix: true } s]
                => GetC1SectorPos5(p, i1, i2, i3, s),
            _ => SectorCoord.Invalid
        });
    }

    public static bool TryGetSectorId(string name, out int sectorid)
    {
        var pos = GetSectorPos(name);

        if (pos.IsValid)
        {
            sectorid = pos.SectorId;
            return true;
        }

        sectorid = default;
        return false;
    }

    private static SectorCoord GetC2SectorPos(FragmentInfo prefix1, FragmentInfo suffix1, FragmentInfo prefix2, FragmentInfo suffix2)
    {
        if (prefix1.IsVowelish == suffix1.IsVowelish || prefix2.IsVowelish == suffix2.IsVowelish)
        {
            return SectorCoord.Invalid;
        }

        int idx0 = prefix1.PrefixOffset + suffix1.SuffixIndex;
        int idx1 = prefix2.PrefixOffset + suffix2.SuffixIndex;
        uint offset = Interleave2((ushort)idx0, (ushort)idx1);
        return SectorCoord.FromOrdinal(offset);
    }

    private static int C1ProcessInfixFragment(FragmentInfo frag, int offset)
        => (Math.DivRem(offset, frag.InfixRunLength, out int infixOffset)
         * (frag.IsVowelish ? _vowelInfixesByOffset.Count : _nonVowelInfixesByOffset.Count))
         + infixOffset
         + frag.InfixOffset;

    private static int C1ProcessPrefixFragment(FragmentInfo frag, int offset)
        => (Math.DivRem(offset, frag.PrefixRunLength, out int prefixOffset)
         * _prefixesByOffset.Count)
         + prefixOffset
         + frag.PrefixOffset;

    private static SectorCoord GetC1SectorPos4(FragmentInfo prefix, FragmentInfo infix1, FragmentInfo infix2, FragmentInfo suffix)
    {
        if (prefix.IsVowelish == infix1.IsVowelish
            || infix1.IsVowelish == infix2.IsVowelish
            || infix2.IsVowelish == suffix.IsVowelish)
        {
            return SectorCoord.Invalid;
        }

        int offset = suffix.SuffixIndex;
        offset = C1ProcessInfixFragment(infix2, offset);
        offset = C1ProcessInfixFragment(infix1, offset);
        offset = C1ProcessPrefixFragment(prefix, offset);

        if (offset > MAX_ORD)
        {
            return SectorCoord.Invalid;
        }

        return SectorCoord.FromOrdinal(offset);
    }

    // This is theoretical, as there are no systems where there would be a third infix
    private static SectorCoord GetC1SectorPos5(FragmentInfo prefix, FragmentInfo infix1, FragmentInfo infix2, FragmentInfo infix3, FragmentInfo suffix)
    {
        if (prefix.IsVowelish == infix1.IsVowelish
            || infix1.IsVowelish == infix2.IsVowelish
            || infix2.IsVowelish == infix3.IsVowelish
            || infix3.IsVowelish == suffix.IsVowelish)
        {
            return SectorCoord.Invalid;
        }

        int offset = suffix.SuffixIndex;
        offset = C1ProcessInfixFragment(infix3, offset);
        offset = C1ProcessInfixFragment(infix2, offset);
        offset = C1ProcessInfixFragment(infix1, offset);
        offset = C1ProcessPrefixFragment(prefix, offset);
        return SectorCoord.FromOrdinal(offset);
    }

    private static SectorCoord GetC1SectorPos3(FragmentInfo prefix, FragmentInfo infix, FragmentInfo suffix)
    {
        if (prefix.IsVowelish == infix.IsVowelish || infix.IsVowelish == suffix.IsVowelish)
        {
            return SectorCoord.Invalid;
        }

        int offset = suffix.SuffixIndex;
        offset = C1ProcessInfixFragment(infix, offset);
        offset = C1ProcessPrefixFragment(prefix, offset);
        return SectorCoord.FromOrdinal(offset);
    }

    private static uint Interleave2(ushort v1, ushort v2)
    {
        unchecked
        {
            // Interleave two 16-bit values into a 32-bit Morton code (Z-order curve).

            if (Bmi2.IsSupported)
            {
                return Bmi2.ParallelBitDeposit(v1, MASK_1X2_U16)
                     | Bmi2.ParallelBitDeposit(v2, MASK_1X2_U16 << 1);
            }
            else if (Vector64<uint>.IsSupported)
            {
                var x = Vector64.Create((uint)v1, (uint)v2);

                x = (x | (x << 8)) & Vector64.Create(MASK_8X2_U16);
                x = (x | (x << 4)) & Vector64.Create(MASK_4X2_U16);
                x = (x | (x << 2)) & Vector64.Create(MASK_2X2_U16);
                x = (x | (x << 1)) & Vector64.Create(MASK_1X2_U16);

                return x[0] | (x[1] << 1);
            }
            else
            {
                var (x1, x2) = ((uint)v1, (uint)v2);

                (x1, x2) = ((x1 | (x1 << 8)) & MASK_8X2_U16, (x2 | (x2 << 8)) & MASK_8X2_U16);
                (x1, x2) = ((x1 | (x1 << 4)) & MASK_4X2_U16, (x2 | (x2 << 4)) & MASK_4X2_U16);
                (x1, x2) = ((x1 | (x1 << 2)) & MASK_2X2_U16, (x2 | (x2 << 2)) & MASK_2X2_U16);
                (x1, x2) = ((x1 | (x1 << 1)) & MASK_1X2_U16, (x2 | (x2 << 1)) & MASK_1X2_U16);

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
                    (ushort)Bmi2.ParallelBitExtract(val, MASK_1X2_U16),
                    (ushort)Bmi2.ParallelBitExtract(val, MASK_1X2_U16 << 1)
                );
            }
            else if (Vector64<uint>.IsSupported)
            {
                var x = Vector64.Create(val, val >> 1) & Vector64.Create(MASK_1X2_U16);

                x = (x | (x >> 1)) & Vector64.Create(MASK_2X2_U16);
                x = (x | (x >> 2)) & Vector64.Create(MASK_4X2_U16);
                x = (x | (x >> 4)) & Vector64.Create(MASK_8X2_U16);
                x = (x | (x >> 8)) & Vector64.Create(MASK_16X2_U16);

                return ((ushort)x[0], (ushort)x[1]);
            }
            else
            {
                var (x1, x2) = (val & MASK_1X2_U16, (val >> 1) & MASK_1X2_U16);

                (x1, x2) = ((x1 | (x1 >> 1)) & MASK_2X2_U16, (x2 | (x2 >> 1)) & MASK_2X2_U16);
                (x1, x2) = ((x1 | (x1 >> 2)) & MASK_4X2_U16, (x2 | (x2 >> 2)) & MASK_4X2_U16);
                (x1, x2) = ((x1 | (x1 >> 4)) & MASK_8X2_U16, (x2 | (x2 >> 4)) & MASK_8X2_U16);

                return ((ushort)((x1 | (x1 >> 8)) & MASK_16X2_U16), (ushort)((x2 | (x2 >> 8)) & MASK_16X2_U16));
            }
        }
    }

    private static uint Interleave3(SectorCoord val)
    {
        unchecked
        {
            // Interleave 3 coordinates (Morton/Z-order)

            if (Bmi2.IsSupported)
            {
                return Bmi2.ParallelBitDeposit((uint)val.X, MASK_1X3_U8)
                     | Bmi2.ParallelBitDeposit((uint)val.Y, MASK_1X3_U8 << 1)
                     | Bmi2.ParallelBitDeposit((uint)val.Z, MASK_1X3_U8 << 2);
            }
            else if (Vector128<uint>.IsSupported)
            {
                var x = Vector128.Create((uint)val.X, (uint)val.Y, (uint)val.Z, 0);

                x = (x | (x << 8)) & Vector128.Create(MASK_4X3_U8);
                x = (x | (x << 4)) & Vector128.Create(MASK_2X3_U8);
                x = (x | (x << 2)) & Vector128.Create(MASK_1X3_U8);

                // Fold the three separated 21-bit lanes together to produce the final
                // 21-bit Morton code (bits ordered x0,y0,z0,x1,y1,z1,...).
                return x[0] | (x[1] << 1) | (x[2] << 2);
            }
            else
            {
                var (x, y, z) = ((uint)val.X, (uint)val.Y, (uint)val.Z);

                (x, y, z) = ((x | (x << 8)) & MASK_4X3_U8, (y | (y << 8)) & MASK_4X3_U8, (z | (z << 8)) & MASK_4X3_U8);
                (x, y, z) = ((x | (x << 4)) & MASK_2X3_U8, (y | (y << 4)) & MASK_2X3_U8, (z | (z << 4)) & MASK_2X3_U8);
                (x, y, z) = ((x | (x << 2)) & MASK_1X3_U8, (y | (y << 2)) & MASK_1X3_U8, (z | (z << 2)) & MASK_1X3_U8);

                return x | (y << 1) | (z << 2);
            }
        }
    }

    private static SectorCoord Deinterleave3(uint val)
    {
        unchecked
        {
            if (Bmi2.IsSupported)
            {
                return new(
                    (sbyte)Bmi2.ParallelBitExtract(val, MASK_1X3_U8),
                    (sbyte)Bmi2.ParallelBitExtract(val, MASK_1X3_U8 << 1),
                    (sbyte)Bmi2.ParallelBitExtract(val, MASK_1X3_U8 << 2)
                );
            }
            else if (Vector128<uint>.IsSupported)
            {
                var x = Vector128.Create(val, val >> 1, val >> 2, 0) & Vector128.Create(MASK_1X3_U8);

                x = (x | (x >> 2)) & Vector128.Create(MASK_2X3_U8);
                x = (x | (x >> 4)) & Vector128.Create(MASK_4X3_U8);
                x = (x | (x >> 8)) & Vector128.Create(MASK_8X3_U8);

                return new((sbyte)x[0], (sbyte)x[1], (sbyte)x[2]);
            }
            else
            {
                var (x, y, z) = (val & MASK_1X3_U8, (val >> 1) & MASK_1X3_U8, (val >> 2) & MASK_1X3_U8);

                (x, y, z) = ((x | (x >> 2)) & MASK_2X3_U8, (y | (y >> 2)) & MASK_2X3_U8, (z | (z >> 2)) & MASK_2X3_U8);
                (x, y, z) = ((x | (x >> 4)) & MASK_4X3_U8, (y | (y >> 4)) & MASK_4X3_U8, (z | (z >> 4)) & MASK_4X3_U8);
                (x, y, z) = ((x | (x >> 8)) & MASK_8X3_U8, (y | (y >> 8)) & MASK_8X3_U8, (z | (z >> 8)) & MASK_8X3_U8);

                return new((sbyte)x, (sbyte)y, (sbyte)z);
            }
        }
    }
}
