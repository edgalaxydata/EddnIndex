namespace EddnIndex.Common.Models;

public record class FilePrefixSchema : IHasId<int>
{
    public int Id { get; init; }
    public required string FilenamePrefix { get; init; }
    public string? PrimarySchema { get; init; }
    public string? EventType { get; init; }
    public bool IsTest { get; init; }
}
