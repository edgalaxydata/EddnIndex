namespace EddnIndex.Common.Models;

public record class SystemName : IHasId<int>
{
    public int Id { get; set; }
    public required string Name { get; init; }

    public virtual bool Equals(SystemName? other) => other?.Name == Name;

    public override int GetHashCode()
        => Name.GetHashCode();
}
