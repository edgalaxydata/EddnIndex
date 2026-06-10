namespace EddnIndexLookup.DTO
{
    /// <summary>
    /// Statistics for a dump directory
    /// </summary>
    public record class DumpDirectoryUsage
    {
        /// <summary>
        /// Directory name
        /// </summary>
        public required string DirectoryName { get; init; }

        /// <summary>
        /// Total size of data in directory
        /// </summary>
        public long DataSize { get; init; }

        /// <summary>
        /// Number of files in directory
        /// </summary>
        public int FileCount { get; init; }
    }
}
