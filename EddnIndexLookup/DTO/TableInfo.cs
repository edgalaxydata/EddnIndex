namespace EddnIndexLookup.DTO
{
    /// <summary>
    /// Table information
    /// </summary>
    public record class TableInfo
    {
        /// <summary>
        /// Name of table
        /// </summary>
        public required string TableName { get; init; }

        /// <summary>
        /// Number of rows
        /// </summary>
        public long RowCount { get; init; }

        /// <summary>
        /// Size of data
        /// </summary>
        public long DataSize { get; init; }

        /// <summary>
        /// Size of indexes
        /// </summary>
        public long IndexSize { get; init; }

        /// <summary>
        /// Total size used by tables
        /// </summary>
        public long TotalSize { get; init; }

        /// <summary>
        /// Average bytes per row
        /// </summary>
        public double BytesPerRow { get; init; }
    }
}
