namespace EddnLookup.DTO;

/// <summary>
/// Base properties for matched items
/// </summary>
public interface IMatchedItem
{
    /// <summary>
    /// Set to true if item details were determined to be invalid
    /// </summary>
    public bool? IsRejected { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date from which details are valid
    /// </summary>
    public DateTimeOffset? ValidFrom { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date until which details were valid
    /// </summary>
    public DateTimeOffset? ValidTo { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was first seen with these details
    /// </summary>
    public DateTimeOffset? FirstSeen { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was last seen with these details
    /// </summary>
    public DateTimeOffset? LastSeen { get; init; }

    /// <summary>
    /// Number of events matching these details
    /// </summary>
    public int? MatchCount { get; init; }

    /// <summary>
    /// Possibly filtered list of events matching these details
    /// </summary>
    public List<MatchEntry>? Matches { get; init; }
}
