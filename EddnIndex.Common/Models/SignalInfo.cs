namespace EddnIndex.Common.Models;

public record class SignalInfo : IHasFirstLastSeen, IHasId<int>
{
    public int Id { get; init; }
    public required string SignalName { get; init; }
    public string? SignalType { get; init; }
    public bool? IsStation { get; init; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? FirstSeen { get; set; }
    public DateTime? LastSeen { get; set; }
}
