namespace EddnIndexLookup.DTO;

/// <summary>
/// Matched event information
/// </summary>
public record class MatchEntry
{
    /// <summary>
    /// EDDN capture filename containing this event
    /// </summary>
    public required string FileName { get; init; }

    /// <summary>
    /// Line number of this event in the EDDN capture file
    /// </summary>
    public int LineNo { get; init; }

    /// <summary>
    /// Entry number within a line (e.g. in a NavRoute entry)
    /// </summary>
    public int? EntryNum { get; init; }

    /// <summary>
    /// Submitting software name
    /// </summary>
    public string? SoftwareName { get; init; }

    /// <summary>
    /// Submitting software version
    /// </summary>
    public string? SoftwareVersion { get; init; }

    /// <summary>
    /// EDDN Event schema
    /// </summary>
    public string? Schema { get; init; }

    /// <summary>
    /// Journal event type
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>
    /// Game Version from Fileheader or LoadGame journal event
    /// </summary>
    public string? GameVersion { get; init; }

    /// <summary>
    /// Game Build from Fileheader or LoadGame journal event
    /// </summary>
    public string? GameBuild { get; init; }

    /// <summary>
    /// System Name for mobile stations (e.g. megaships and fleet carriers)
    /// </summary>
    public string? SystemName { get; init; }

    /// <summary>
    /// SystemAddress for mobile stations (e.g. megaships and fleet carriers)
    /// </summary>
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Body Name
    /// </summary>
    /// <example>Rigel</example>
    public string? BodyName { get; init; }

    /// <summary>
    /// Unique ID of station
    /// </summary>
    public long? MarketId { get; init; }

    /// <summary>
    /// Name of station
    /// </summary>
    public string? StationName { get; init; }

    /// <summary>
    /// True if game mode was Odyssey or later (i.e. on-foot actions are possible)
    /// </summary>
    public bool? IsOdyssey { get; init; }

    /// <summary>
    /// True if game mode was Horizons or later (i.e. planetary landings are possible)
    /// </summary>
    public bool? IsHorizons { get; init; }

    /// <summary>
    /// UTC Event timestamp from event source (e.g. journal)
    /// </summary>
    public DateTime? Timestamp { get; init; }

    /// <summary>
    /// UTC Timestamp when message was received by EDDN gateway
    /// </summary>
    public DateTime? GatewayTimestamp { get; init; }

    /// <summary>
    /// URL to extract event JSON
    /// </summary>
    public string? Extract { get; init; }

    /// <summary>
    /// Internal System Id
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int? SystemId { get; init; }

    /// <summary>
    /// Internal Body Id
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public long? BodyId { get; init; }

    /// <summary>
    /// Internal Station Id
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int? StationId { get; init; }
}
