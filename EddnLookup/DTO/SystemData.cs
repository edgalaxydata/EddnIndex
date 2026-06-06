namespace EddnLookup.DTO;

/// <summary>
/// System details
/// </summary>
public record class SystemData : IMatchedItem
{
    /// <summary>
    /// Name of system
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Procedurally generated name of system where available
    /// </summary>
    public string? PGName { get; init; }

    /// <summary>
    /// Unique identifier for system from event
    /// </summary>
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Unique identifier based on system name
    /// </summary>
    public long? NameSystemAddress { get; init; }

    /// <summary>
    /// Heliocentric galactic rectangular coordinates of system in lightyears
    /// </summary>
    public Coords? Coords { get; init; }

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

    /// <summary>
    /// Bodies from events seen with these system details
    /// </summary>
    public List<BodyData>? Bodies { get; init; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    internal int Id { get; init; }
}
