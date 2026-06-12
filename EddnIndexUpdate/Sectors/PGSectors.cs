namespace EddnIndexUpdate.Sectors;

public static class PGSectors
{
    [System.Diagnostics.DebuggerDisplay("({X},{Y},{Z})")]
    public readonly struct ByteXYZ(sbyte x, sbyte y, sbyte z) : IComparable<ByteXYZ>
    {
        public readonly sbyte X = x;
        public readonly sbyte Y = y;
        public readonly sbyte Z = z;

        public readonly bool IsValid => X >= 0 && Y >= 0 && Y < 64 && Z >= 0;

        public readonly int Ord => IsValid ? X + Y * 128 + Z * 16384 : -1;

        public readonly int SectorId => IsValid ? X + Y * 128 + Z * 8192 : -1;

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

        public static readonly ByteXYZ Invalid = new(-128, -128, -128);
    }

    private struct FragmentInfo
    {
        public string Value;
        public bool IsPrefix;
        public bool IsC1VowelPrefix;
        public bool IsC2VowelPrefix;
        public int PrefixIndex;
        public bool IsInfix;
        public bool IsVowelInfix;
        public int InfixIndex;
        public bool IsSuffix;
        public bool IsVowelSuffix;
        public int SuffixIndex;
    }

    // Tables of prefixes, infixes and suffixes from https://bitbucket.org/Esvandiary/edts/src/master/pgdata.py
    // Prefixes
    private static readonly string[] Prefixes =
    [
        "Th", "Eo", "Oo", "Eu", "Tr", "Sly", "Dry", "Ou",
        "Tz", "Phl", "Ae", "Sch", "Hyp", "Syst", "Ai", "Kyl",
        "Phr", "Eae", "Ph", "Fl", "Ao", "Scr", "Shr", "Fly",
        "Pl", "Fr", "Au", "Pry", "Pr", "Hyph", "Py", "Chr",
        "Phyl", "Tyr", "Bl", "Cry", "Gl", "Br", "Gr", "By",
        "Aae", "Myc", "Gyr", "Ly", "Myl", "Lych", "Myn", "Ch",
        "Myr", "Cl", "Rh", "Wh", "Pyr", "Cr", "Syn", "Str",
        "Syr", "Cy", "Wr", "Hy", "My", "Sty", "Sc", "Sph",
        "Spl", "A", "Sh", "B", "C", "D", "Sk", "Io",
        "Dr", "E", "Sl", "F", "Sm", "G", "H", "I",
        "Sp", "J", "Sq", "K", "L", "Pyth", "M", "St",
        "N", "O", "Ny", "Lyr", "P", "Sw", "Thr", "Lys",
        "Q", "R", "S", "T", "Ea", "U", "V", "W",
        "Schr", "X", "Ee", "Y", "Z", "Ei", "Oe",
    ];

    // Vowelish infixes
    private static readonly string[] Infixes1 =
    [
        "o", "ai", "a", "oi", "ea", "ie", "u", "e",
        "ee", "oo", "ue", "i", "oa", "au", "ae", "oe"
    ];

    // Consonantish infixes
    private static readonly string[] Infixes2 =
    [
        "ll", "ss", "b", "c", "d", "f", "dg", "g",
        "ng", "h", "j", "k", "l", "m", "n", "mb",
        "p", "q", "gn", "th", "r", "s", "t", "ch",
        "tch", "v", "w", "wh", "ck", "x", "y", "z",
        "ph", "sh", "ct", "wr"
    ];

    // Vowelish suffixes
    private static readonly string[] Suffixes1 =
    [
        "oe",  "io",  "oea", "oi",  "aa",  "ua", "eia", "ae",
        "ooe", "oo",  "a",   "ue",  "ai",  "e",  "iae", "oae",
        "ou",  "uae", "i",   "ao",  "au",  "o",  "eae", "u",
        "aea", "ia",  "ie",  "eou", "aei", "ea", "uia", "oa",
        "aae", "eau", "ee"
    ];

    // Consonantish suffixes
    private static readonly string[] Suffixes2 =
    [
        "b", "scs", "wsy", "c", "d", "vsky", "f", "sms",
        "dst", "g", "rb", "h", "nts", "ch", "rd", "rld",
        "k", "lls", "ck", "rgh", "l", "rg", "m", "n",
        // Formerly sequence 4/5...
        "hm", "p", "hn", "rk", "q", "rl", "r", "rm",
        "s", "cs", "wyg", "rn", "ct", "t", "hs", "rbs",
        "rp", "tts", "v", "wn", "ms", "w", "rr", "mt",
        "x", "rs", "cy", "y", "rt", "z", "ws", "lch", // "y" is speculation
        "my", "ry", "nks", "nd", "sc", "ng", "sh", "nk",
        "sk", "nn", "ds", "sm", "sp", "ns", "nt", "dy",
        "ss", "st", "rrs", "xt", "nz", "sy", "xy", "rsch",
        "rphs", "sts", "sys", "sty", "th", "tl", "tls", "rds",
        "nch", "rns", "ts", "wls", "rnt", "tt", "rdy", "rst",
        "pps", "tz", "tch", "sks", "ppy", "ff", "sps", "kh",
        "sky", "ph", "lts", "wnst", "rth", "ths", "fs", "pp",
        "ft", "ks", "pr", "ps", "pt", "fy", "rts", "ky",
        "rshch", "mly", "py", "bb", "nds", "wry", "zz", "nns",
        "ld", "lf", "gh", "lks", "sly", "lk", "ll", "rph",
        "ln", "bs", "rsts", "gs", "ls", "vvy", "lt", "rks",
        "qs", "rps", "gy", "wns", "lz", "nth", "phs"
    ];

    // Vowelish C2 prefixes
    private static readonly HashSet<string> C2PrefixSuffix2 = new(
    [
        "Eo", "Oo", "Eu", "Ou", "Ae", "Ai", "Eae", "Ao", "Au", "Aae"
    ], StringComparer.InvariantCultureIgnoreCase);

    // Vowelish C1 prefixes
    private static readonly HashSet<string> C1PrefixInfix2 = new(
    [
        "Eo", "Oo", "Eu", "Ou", "Ae", "Ai", "Eae", "Ao",
        "Au", "Aae", "A", "Io", "E", "I", "O", "Ea",
        "U", "Ee", "Ei", "Oe"
    ], StringComparer.InvariantCultureIgnoreCase);

    // Prefixes using short run lengths
    private static readonly Dictionary<string, int> PrefixRunLengths = new(StringComparer.InvariantCultureIgnoreCase)
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
    private static readonly Dictionary<string, int> InfixRunLengths = new(StringComparer.InvariantCultureIgnoreCase)
    {
        // Sequence 1
        { "oi",   88 }, { "ue",  147 }, { "oa",   57 },
        { "au",  119 }, { "ae",   12 }, { "oe",   39 },
        // Sequence 2
        { "dg",   31 }, { "tch",  20 }, { "wr",   31 },
    };

    private static readonly FragmentInfo[] Fragments = FillFragments(Prefixes, Infixes1, Infixes2, Suffixes1, Suffixes2);

    private static readonly Dictionary<string, int> PrefixOffsets = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly int PrefixTotalRunLength = FillOffsets(Prefixes, PrefixRunLengths, PrefixOffsets, 35);

    private static readonly Dictionary<string, int> InfixOffsets = new(StringComparer.InvariantCultureIgnoreCase);
    private static readonly int Infix1TotalRunLength = FillOffsets(Infixes1, InfixRunLengths, InfixOffsets, Suffixes2.Length);
    private static readonly int Infix2TotalRunLength = FillOffsets(Infixes2, InfixRunLengths, InfixOffsets, Suffixes1.Length);

    private static readonly Dictionary<ByteXYZ, string> CachedSectorsByCoords = [];
    private static readonly Dictionary<string, ByteXYZ> CachedSectorsByName = new(StringComparer.InvariantCultureIgnoreCase);

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

    private static FragmentInfo[] FillFragments(string[] prefixes, string[] infixes1, string[] infixes2, string[] suffixes1, string[] suffixes2)
    {
        Dictionary<string, FragmentInfo> frags = new(StringComparer.InvariantCultureIgnoreCase);

        for (int i = 0; i < prefixes.Length; i++)
        {
            string prefix = prefixes[i];
            string p = prefix.ToLowerInvariant();
            FragmentInfo frag = frags.TryGetValue(p, out FragmentInfo value) ? value : new FragmentInfo { Value = p };
            frag.IsPrefix = true;
            frag.IsC1VowelPrefix = C1PrefixInfix2.Contains(prefix);
            frag.IsC2VowelPrefix = C2PrefixSuffix2.Contains(prefix);
            frag.PrefixIndex = i;
            frags[p] = frag;
        }

        for (int i = 0; i < infixes1.Length; i++)
        {
            string p = infixes1[i].ToLowerInvariant();
            FragmentInfo frag = frags.TryGetValue(p, out FragmentInfo value) ? value : new FragmentInfo { Value = p };
            frag.IsInfix = true;
            frag.IsVowelInfix = true;
            frag.InfixIndex = i;
            frags[p] = frag;
        }

        for (int i = 0; i < infixes2.Length; i++)
        {
            string p = infixes2[i].ToLowerInvariant();
            FragmentInfo frag = frags.TryGetValue(p, out FragmentInfo value) ? value : new FragmentInfo { Value = p };
            frag.IsInfix = true;
            frag.IsVowelInfix = false;
            frag.InfixIndex = i;
            frags[p] = frag;
        }

        for (int i = 0; i < suffixes1.Length; i++)
        {
            string p = suffixes1[i].ToLowerInvariant();
            FragmentInfo frag = frags.TryGetValue(p, out FragmentInfo value) ? value : new FragmentInfo { Value = p };
            frag.IsSuffix = true;
            frag.IsVowelSuffix = true;
            frag.SuffixIndex = i;
            frags[p] = frag;
        }

        for (int i = 0; i < suffixes2.Length; i++)
        {
            string p = suffixes2[i].ToLowerInvariant();
            FragmentInfo frag = frags.TryGetValue(p, out FragmentInfo value) ? value : new FragmentInfo { Value = p };
            frag.IsSuffix = true;
            frag.IsVowelSuffix = false;
            frag.SuffixIndex = i;
            frags[p] = frag;
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

        int offset = (pos.Z << 14) + (pos.Y << 7) + pos.X;
        return CachedSectorsByCoords[pos] = IsC1Sector(offset) ? GetC1Name(offset) : GetC2Name(offset);
    }

    public static string GetSectorName(int sectorid)
    {
        var pos = new ByteXYZ(
            (sbyte)(sectorid & 0x7F),
            (sbyte)(sectorid >> 7 & 0x3F),
            (sbyte)(sectorid >> 13 & 0x7F)
        );
        return GetSectorName(pos);
    }

    public static string GetC1SectorName(ByteXYZ pos)
    {
        return GetC1Name((pos.Z << 14) + (pos.Y << 7) + pos.X);
    }

    public static string GetC2SectorName(ByteXYZ pos)
    {
        return GetC2Name((pos.Z << 14) + (pos.Y << 7) + pos.X);
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

    private static string GetC1Name(int offset)
    {
        List<string> frags = [];
        int prefix_cnt = Math.DivRem(offset, PrefixTotalRunLength, out int cur_offset);
        string prefix = Prefixes.Last(p => PrefixOffsets[p] <= cur_offset);
        frags.Add(prefix);
        cur_offset -= PrefixOffsets[prefix];
        bool infix1s2 = C1PrefixInfix2.Contains(prefix);
        int infix1_total_len = infix1s2 ? Infix2TotalRunLength : Infix1TotalRunLength;
        string[] infix1s = infix1s2 ? Infixes2 : Infixes1;
        int infix1_cnt = Math.DivRem(prefix_cnt * PrefixRunLengths[prefix] + cur_offset, infix1_total_len, out cur_offset);
        string infix1 = infix1s.Last(p => InfixOffsets[p] <= cur_offset);
        frags.Add(infix1);
        cur_offset -= InfixOffsets[infix1];
        int infix1_run_len = InfixRunLengths[infix1];
        string[] suffixes = infix1s2 ? Suffixes1 : Suffixes2;
        int next_idx = infix1_run_len * infix1_cnt + cur_offset;

        if (next_idx >= suffixes.Length)
        {
            bool infix2s2 = !infix1s2;
            int infix2_total_len = infix2s2 ? Infix2TotalRunLength : Infix1TotalRunLength;
            int infix2_cnt = Math.DivRem(next_idx, infix2_total_len, out cur_offset);
            string[] infix2s = infix2s2 ? Infixes2 : Infixes1;
            string infix2 = infix2s.Last(p => InfixOffsets[p] <= cur_offset);
            frags.Add(infix2);
            cur_offset -= InfixOffsets[infix2];
            int infix2_run_len = InfixRunLengths[infix2];
            suffixes = infix2s2 ? Suffixes1 : Suffixes2;
            next_idx = infix2_run_len * infix2_cnt + cur_offset;
        }

        if (next_idx >= suffixes.Length)
        {
            throw new InvalidOperationException("Bad C1 name offset");
        }

        frags.Add(suffixes[next_idx]);
        return string.Concat(frags);
    }

    private static string GetC2Name(int offset)
    {
        Tuple<ushort, ushort> cur_idx = Deinterleave2((uint)offset);
        string p1 = Prefixes.Last(p => PrefixOffsets[p] <= cur_idx.Item1);
        string p2 = Prefixes.Last(p => PrefixOffsets[p] <= cur_idx.Item2);
        string[] s1s = C2PrefixSuffix2.Contains(p1) ? Suffixes2 : Suffixes1;
        string[] s2s = C2PrefixSuffix2.Contains(p2) ? Suffixes2 : Suffixes1;
        string s1 = s1s[cur_idx.Item1 - PrefixOffsets[p1]];
        string s2 = s2s[cur_idx.Item2 - PrefixOffsets[p2]];
        return $"{p1}{s1} {p2}{s2}";
    }

    private static List<FragmentInfo>? GetSectorFragments(string name)
    {
        name = name.ToLowerInvariant();
        List<FragmentInfo> fragments = [];
        ReadOnlySpan<char> current = name;

        while (current != "")
        {
            bool spacestart = current.StartsWith(" ");
            current = current.Trim();

            FragmentInfo frag = default;

            foreach (var item in Fragments)
            {
                if (current.StartsWith(item.Value))
                {
                    frag = item;
                    break;
                }
            }

            if (frag.Value == null)
            {
                return null;
            }

            if (spacestart)
            {
                frag.IsSuffix = false;
                frag.IsInfix = false;
            }
            else if (fragments.Count != 0 && frag.IsInfix && frag.IsVowelInfix != fragments.Last().IsVowelInfix)
            {
                frag.IsPrefix = false;
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

        List<FragmentInfo>? fragments = GetSectorFragments(name);
        ByteXYZ coords;

        if (fragments == null)
        {
            return ByteXYZ.Invalid;
        }
        else if (fragments.Count == 4 && fragments[0].IsPrefix && fragments[1].IsSuffix && fragments[2].IsPrefix && fragments[3].IsSuffix)
        {
            coords = GetC2SectorPos(fragments);
        }
        else if (fragments.Count == 3 && fragments[0].IsPrefix && fragments[1].IsInfix && fragments[2].IsSuffix)
        {
            coords = GetC1SectorPos3(fragments);
        }
        else if (fragments.Count == 4 && fragments[0].IsPrefix && fragments[1].IsInfix && fragments[2].IsInfix && fragments[3].IsSuffix)
        {
            coords = GetC1SectorPos4(fragments);
        }
        else
        {
            return ByteXYZ.Invalid;
        }

        CachedSectorsByName[name] = coords;
        return coords;
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

    private static ByteXYZ GetC2SectorPos(List<FragmentInfo> fragments)
    {
        if (fragments[0].IsC2VowelPrefix == fragments[1].IsVowelSuffix || fragments[2].IsC2VowelPrefix == fragments[3].IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int idx0 = PrefixOffsets[fragments[0].Value] + fragments[1].SuffixIndex;
        int idx1 = PrefixOffsets[fragments[2].Value] + fragments[3].SuffixIndex;
        uint offset = Interleave2((ushort)idx0, (ushort)idx1);
        return new ByteXYZ((sbyte)(offset & 0x7F), (sbyte)(offset >> 7 & 0x7F), (sbyte)(offset >> 14 & 0x7F));
    }

    private static int C1ProcessInfixFragment(FragmentInfo frag, int offset)
    {
        offset = Math.DivRem(offset, InfixRunLengths[frag.Value], out int offset_mod);
        offset *= frag.IsVowelInfix ? Infix1TotalRunLength : Infix2TotalRunLength;
        offset += offset_mod;
        offset += InfixOffsets[frag.Value];
        return offset;
    }

    private static int C1ProcessPrefixFragment(FragmentInfo frag, int offset)
    {
        offset = Math.DivRem(offset, PrefixRunLengths[frag.Value], out int offset_mod);
        offset *= PrefixTotalRunLength;
        offset += offset_mod;
        offset += PrefixOffsets[frag.Value];
        return offset;
    }

    private static ByteXYZ GetC1SectorPos4(List<FragmentInfo> fragments)
    {
        if (fragments[0].IsC1VowelPrefix == fragments[1].IsVowelInfix || fragments[1].IsVowelInfix == fragments[2].IsVowelInfix || fragments[2].IsVowelInfix == fragments[3].IsVowelSuffix)
        {
            return ByteXYZ.Invalid;
        }

        int offset = fragments[3].SuffixIndex;
        offset += offset / InfixRunLengths[fragments[2].Value] * (fragments[2].IsVowelInfix ? Infix1TotalRunLength : Infix2TotalRunLength);

        offset = C1ProcessInfixFragment(fragments[2], offset);
        offset = C1ProcessInfixFragment(fragments[1], offset);
        offset = C1ProcessPrefixFragment(fragments[0], offset);
        return new ByteXYZ((sbyte)(offset & 0x7F), (sbyte)(offset >> 7 & 0x7F), (sbyte)(offset >> 14 & 0x7F));
    }

    private static ByteXYZ GetC1SectorPos3(List<FragmentInfo> fragments)
    {
        if (fragments[0].IsC1VowelPrefix == fragments[1].IsVowelInfix || fragments[1].IsVowelInfix == fragments[2].IsVowelSuffix)
        {
            //return ByteXYZ.Invalid;
        }

        int offset = fragments[2].SuffixIndex;
        offset = C1ProcessInfixFragment(fragments[1], offset);
        offset = C1ProcessPrefixFragment(fragments[0], offset);
        return new ByteXYZ((sbyte)(offset & 0x7F), (sbyte)(offset >> 7 & 0x7F), (sbyte)(offset >> 14 & 0x7F));
    }

    private static uint Interleave2(ushort v1, ushort v2)
    {
        unchecked
        {
            ulong x = v1 | (ulong)v2 << 32;
            x = (x | x << 8) & 0x00FF00FF00FF00FFUL;
            x = (x | x << 4) & 0x0F0F0F0F0F0F0F0FUL;
            x = (x | x << 2) & 0x3333333333333333UL;
            x = (x | x << 1) & 0x5555555555555555UL;
            return (uint)((x | x >> 31) & 0xFFFFFFFF);
        }
    }

    private static Tuple<ushort, ushort> Deinterleave2(uint val)
    {
        unchecked
        {
            ulong x = val & 0x55555555UL | (val & 0xAAAAAAAAUL) << 31;
            x = (x | x >> 1) & 0x3333333333333333UL;
            x = (x | x >> 2) & 0x0F0F0F0F0F0F0F0FUL;
            x = (x | x >> 4) & 0x00FF00FF00FF00FFUL;
            x = (x | x >> 8) & 0x0000FFFF0000FFFFUL;
            return new Tuple<ushort, ushort>((ushort)(x & 0xFFFF), (ushort)(x >> 32 & 0xFFFF));
        }
    }

    private static uint Interleave3(ByteXYZ val)
    {
        unchecked
        {
            ulong x = (ulong)val.X & 0x7F | ((ulong)val.Y & 0x7F) << 7 | ((ulong)val.Z & 0x7F) << 14;
            x = (x | x << 32) & 0x001F00000000FFFFUL;
            x = (x | x << 16) & 0x001F0000FF0000FFUL;
            x = (x | x << 8) & 0x100F00F00F00F00FUL;
            x = (x | x << 4) & 0x10C30C30C30C30C3UL;
            x = (x | x << 2) & 0x1249249249249249UL;
            return (uint)((x | x >> 20 | x >> 40) & 0x1FFFFF);
        }
    }

    private static ByteXYZ Deinterleave3(uint val)
    {
        unchecked
        {
            ulong x = (ulong)val & 0x49249 | ((ulong)val & 0x92492) << 20 | ((ulong)val & 0x124924) << 40;
            x = (x | x >> 2) & 0x10C30C30C30C30C3UL;
            x = (x | x >> 4) & 0x100F00F00F00F00FUL;
            x = (x | x >> 8) & 0x001F0000FF0000FFUL;
            x = (x | x >> 16) & 0x001F00000000FFFFUL;
            x = (x | x >> 32) & 0x00000000001FFFFFUL;
            return new ByteXYZ((sbyte)(x & 0x7F), (sbyte)(x >> 7 & 0x7F), (sbyte)(x >> 14 & 0x7F));
        }
    }
}
