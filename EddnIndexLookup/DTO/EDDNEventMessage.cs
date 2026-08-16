using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// EDDN event body
/// </summary>
[DataContract]
public class EDDNEventMessage
{
    /// <summary>
    /// Journal event type
    /// </summary>
    [DataMember(Name = "event")]
    public string? Event { get; init; }

    /// <summary>
    /// True if game mode was Horizons or later (i.e. planetary landings are possible)
    /// </summary>
    [DataMember(Name = "horizons")]
    public bool? Horizons { get; init; }

    /// <summary>
    /// True if game mode was Odyssey or later (i.e. on-foot actions are possible)
    /// </summary>
    [DataMember(Name = "odyssey")]
    public bool? Odyssey { get; init; }

    /// <summary>
    /// UTC Event timestamp from event source (e.g. journal)
    /// </summary>
    [DataMember(Name = "timestamp")]
    public DateTime? Timestamp { get; init; }
}
