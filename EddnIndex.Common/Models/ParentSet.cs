namespace EddnIndex.Common.Models;

public record class ParentSet : IHasId<int>
{
    public int Id { get; init; }
    public int? BodyID { get; init; }
    public string? BodyType { get; init; }
    public int? ParentSetId { get; init; }
    public string? ParentJson { get; init; }

    public ParentSet? Parent { get; init; }

    public virtual bool Equals(ParentSet? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(other, this)) return true;

        return this.BodyID == other.BodyID
            && this.BodyType == other.BodyType
            && this.ParentSetId == other.ParentSetId
            && this.ParentJson == other.ParentJson;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BodyID, BodyType, ParentSetId, ParentJson);
    }
}
