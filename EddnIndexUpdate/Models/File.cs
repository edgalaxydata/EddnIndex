namespace EddnIndexUpdate.Models;

public record class File : IHasId<int>
{
    public int Id { get; set; }
    public required string FileName { get; init; }
    public DateOnly? Date { get; init; }
    public string? PrimarySchema { get; init; }
    public string? EventType { get; init; }
    public int? LineCount { get; set; }
    public long? CompressedSize { get; set; }
    public long? UncompressedSize { get; set; }
    public int? SystemLineCount { get; set; }
    public int? StationLineCount { get; set; }
    public int? NavRouteSystemCount { get; set; }
    public int? BodyLineCount { get; set; }
    public int? SignalCount { get; set; }
    public int? BodySignalCount { get; set; }
    public int? ErrorCount { get; set; }
    public bool? IsTest { get; set; }
    public int? ProcessedVersion { get; set; }
    public int? PrimarySchemaEventId { get; set; }

    public SchemaEventInfo? PrimarySchemaEvent { get; set; }
}
