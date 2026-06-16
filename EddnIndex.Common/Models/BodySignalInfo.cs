namespace EddnIndex.Common.Models;

public record class BodySignalInfo : IHasFirstLastSeen, IHasId<int>
{
    public int Id { get; init; }
    public required string SignalType { get; init; }
    public string? Category { get; init; }
    public string? SubCategory { get; init; }
    public long? EntryID { get; init; }
    public string? Region { get; init; }
    public int? SignalCount { get; init; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }
}
