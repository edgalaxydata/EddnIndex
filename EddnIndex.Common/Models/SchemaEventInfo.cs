namespace EddnIndex.Common.Models;

public class SchemaEventInfo : IHasFirstLastSeen, IHasId<int>
{
    public int Id { get; set; }
    public required string Schema { get; init; }
    public string? EventType { get; init; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }
}
