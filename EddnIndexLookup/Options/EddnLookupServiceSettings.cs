namespace EddnIndexLookup.Options
{
    /// <summary>
    /// Settings for EddnLookupService
    /// </summary>
    public class EddnLookupServiceSettings
    {
        /// <summary>
        /// Path to indexed EDDN captures
        /// </summary>
        public string? IndexedDir { get; set; }

        /// <summary>
        /// Dump directories to enumerate for tableinfo
        /// </summary>
        public Dictionary<string, string> DumpDirs { get; set; } = [];
    }
}
