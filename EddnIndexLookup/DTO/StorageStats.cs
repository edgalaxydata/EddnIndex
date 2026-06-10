namespace EddnIndexLookup.DTO
{
    /// <summary>
    /// Storage statistics
    /// </summary>
    public class StorageStats
    {
        /// <summary>
        /// Table statistics
        /// </summary>
        public Dictionary<string, TableInfo> Tables { get; init; } = [];

        /// <summary>
        /// Storage used by dumps
        /// </summary>
        public Dictionary<string, DumpDirectoryUsage> DumpUsages { get; init; } = [];
    }
}
