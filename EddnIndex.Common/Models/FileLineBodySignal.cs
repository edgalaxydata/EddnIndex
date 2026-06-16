namespace EddnIndex.Common.Models;

public record class FileLineBodySignal
{
    public int FileId { get; init; }
    public int LineNo { get; init; }
    public int EntryNum { get; init; }
    public int BodySignalId { get; init; }
    public long? BodyId { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public DateTime? GatewayTimestamp { get; init; }

    public BodySignalInfo? Signal { get; init; }
    public BodyInfo? Body { get; init; }
}
