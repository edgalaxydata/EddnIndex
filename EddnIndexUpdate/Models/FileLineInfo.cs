namespace EddnIndexUpdate.Models
{
    public record class FileLineInfo
    {
        public int FileId { get; init; }
        public int LineNo { get; init; }
        public int LineLength { get; init; }
        public int? SoftwareId { get; init; }
        public int? SystemId { get; init; }
        public int? GameVersionId { get; init; }
        public DateTime? Timestamp { get; init; }
        public DateTime? GatewayTimestamp { get; init; }
        public bool? IsBad { get; init; }
        public int? ProcessedVersion { get; init; }
        public int? SchemaEventId { get; init; }

        public SoftwareInfo? Software { get; init; }
        public System? System { get; init; }
        public GameVersionInfo? GameVersion { get; init; }
        public SchemaEventInfo? SchemaEvent { get; init; }
    }
}
