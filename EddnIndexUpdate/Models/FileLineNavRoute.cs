namespace EddnIndexUpdate.Models
{
    public record class FileLineNavRoute
    {
        public int FileId { get; init; }
        public int LineNo { get; init; }
        public int EntryNum { get; init; }
        public int SystemId { get; init; }
        public DateTime? GatewayTimestamp { get; init; }

        public System? System { get; init; }
    }
}
