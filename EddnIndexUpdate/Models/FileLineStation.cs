namespace EddnIndexUpdate.Models;

public record class FileLineStation
{
    public int FileId { get; init; }
    public int LineNo { get; init; }
    public int StationId { get; init; }
    public DateTime? GatewayTimestamp { get; init; }
    public short? LatitudeError { get; init; }
    public short? LongitudeError { get; init; }

    public StationInfo? Station { get; init; }
}
