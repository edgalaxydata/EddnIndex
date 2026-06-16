namespace EddnIndex.Common.Models;

public record class BodyName : IHasId<int>
{
    public int Id { get; init; }
    public required string Name { get; init; }

    public virtual bool Equals(BodyName? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return this.Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
