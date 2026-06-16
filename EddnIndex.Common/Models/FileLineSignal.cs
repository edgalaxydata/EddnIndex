namespace EddnIndex.Common.Models;

public record class FileLineSignal
{
    public int FileId { get; init; }
    public int LineNo { get; init; }
    public int SignalSetId { get; init; }
    public int? SystemId { get; init; }
    public DateTime? GatewayTimestamp { get; init; }

    public SignalInfoSet? SignalInfoSet { get; init; }
    public SystemInfo? System { get; init; }
}
