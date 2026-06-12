using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Matched event information
/// </summary>
[DataContract]
public record class MatchEntry
{
    /// <summary>
    /// EDDN capture filename containing this event
    /// </summary>
    [DataMember(Name = "FileName", IsRequired = true)]
    public string? FileName { get; init; }

    /// <summary>
    /// Line number of this event in the EDDN capture file
    /// </summary>
    [DataMember(Name = "LineNo", IsRequired = true)]
    public int LineNo { get; init; }

    /// <summary>
    /// Entry number within a line (e.g. in a NavRoute entry)
    /// </summary>
    [DataMember(Name = "EntryNum")]
    public int? EntryNum { get; init; }

    /// <summary>
    /// Submitting software name
    /// </summary>
    [DataMember(Name = "SoftwareName")]
    public string? SoftwareName { get; init; }

    /// <summary>
    /// Submitting software version
    /// </summary>
    [DataMember(Name = "SoftwareVersion")]
    public string? SoftwareVersion { get; init; }

    /// <summary>
    /// EDDN Event schema
    /// </summary>
    [DataMember(Name = "Schema")]
    public string? Schema { get; init; }

    /// <summary>
    /// Journal event type
    /// </summary>
    [DataMember(Name = "EventType")]
    public string? EventType { get; init; }

    /// <summary>
    /// Game Version from Fileheader or LoadGame journal event
    /// </summary>
    [DataMember(Name = "GameVersion")]
    public string? GameVersion { get; init; }

    /// <summary>
    /// Game Build from Fileheader or LoadGame journal event
    /// </summary>
    [DataMember(Name = "GameBuild")]
    public string? GameBuild { get; init; }

    /// <summary>
    /// System Name for mobile stations (e.g. megaships and fleet carriers)
    /// </summary>
    [DataMember(Name = "SystemName")]
    public string? SystemName { get; init; }

    /// <summary>
    /// SystemAddress for mobile stations (e.g. megaships and fleet carriers)
    /// </summary>
    [DataMember(Name = "SystemAddress")]
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Body Name
    /// </summary>
    /// <example>Rigel</example>
    [DataMember(Name = "BodyName")]
    public string? BodyName { get; init; }

    /// <summary>
    /// Unique ID of station
    /// </summary>
    [DataMember(Name = "MarketId")]
    public long? MarketId { get; init; }

    /// <summary>
    /// Name of station
    /// </summary>
    [DataMember(Name = "StationName")]
    public string? StationName { get; init; }

    /// <summary>
    /// True if game mode was Odyssey or later (i.e. on-foot actions are possible)
    /// </summary>
    [DataMember(Name = "IsOdyssey")]
    public bool? IsOdyssey { get; init; }

    /// <summary>
    /// True if game mode was Horizons or later (i.e. planetary landings are possible)
    /// </summary>
    [DataMember(Name = "IsHorizons")]
    public bool? IsHorizons { get; init; }

    /// <summary>
    /// UTC Event timestamp from event source (e.g. journal)
    /// </summary>
    [DataMember(Name = "Timestamp")]
    public DateTime? Timestamp { get; init; }

    /// <summary>
    /// UTC Timestamp when message was received by EDDN gateway
    /// </summary>
    [DataMember(Name = "GatewayTimestamp")]
    public DateTime? GatewayTimestamp { get; init; }

    /// <summary>
    /// URL to extract event JSON
    /// </summary>
    [DataMember(Name = "Extract")]
    public string? Extract { get; init; }

    /// <summary>
    /// Internal System Id
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public int? SystemId { get; init; }

    /// <summary>
    /// Internal Body Id
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public long? BodyId { get; init; }

    /// <summary>
    /// Internal Station Id
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public int? StationId { get; init; }
}
