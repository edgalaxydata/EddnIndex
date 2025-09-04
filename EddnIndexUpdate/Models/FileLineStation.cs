namespace EddnIndexUpdate.Models
{
    public record class FileLineStation
    {
        public int FileId { get; init; }
        public int LineNo { get; init; }
        public int StationId { get; init; }
        public DateTime? GatewayTimestamp { get; init; }

        public Station? Station { get; init; }
    }
}
