using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// System details
/// </summary>
[DataContract]
public record class SystemData : IMatchedItem, ISystemData
{
    /// <summary>
    /// Name of system
    /// </summary>
    [Required]
    [DataMember(Name = "Name")]
    public string Name { get; init; } = "";

    /// <summary>
    /// Procedurally generated name of system where available
    /// </summary>
    [DataMember(Name = "PGName")]
    public string? PGName { get; init; }

    /// <summary>
    /// Unique identifier for system from event
    /// </summary>
    [DataMember(Name = "SystemAddress")]
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Unique identifier based on system name
    /// </summary>
    [DataMember(Name = "NameSystemAddress")]
    public long? NameSystemAddress { get; init; }

    /// <summary>
    /// Heliocentric galactic rectangular coordinates of system in lightyears
    /// </summary>
    [DataMember(Name = "Coords")]
    public Coords? Coords { get; init; }

    /// <summary>
    /// Set to true if item details were determined to be invalid
    /// </summary>
    [DataMember(Name = "IsRejected")]
    public bool? IsRejected { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date from which details are valid
    /// </summary>
    [DataMember(Name = "ValidFrom")]
    public DateTime? ValidFrom { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date until which details were valid
    /// </summary>
    [DataMember(Name = "ValidTo")]
    public DateTime? ValidTo { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was first seen with these details
    /// </summary>
    [DataMember(Name = "FirstSeen")]
    public DateTime? FirstSeen { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was last seen with these details
    /// </summary>
    [DataMember(Name = "LastSeen")]
    public DateTime? LastSeen { get; init; }

    /// <summary>
    /// Number of events matching these details
    /// </summary>
    [DataMember(Name = "MatchCount")]
    public int? MatchCount { get; init; }

    /// <summary>
    /// Possibly filtered list of events matching these details
    /// </summary>
    [DataMember(Name = "Matches")]
    public List<MatchEntry>? Matches { get; init; }

    /// <summary>
    /// Bodies from events seen with these system details
    /// </summary>
    [DataMember(Name = "Bodies")]
    public List<SystemBodyData>? Bodies { get; init; }

    /// <summary>
    /// Stations from events matching these details
    /// </summary>
    [DataMember(Name = "Stations")]
    public List<StationData>? Stations { get; init; }

    /// <summary>
    /// Internal system id
    /// </summary>
    [IgnoreDataMember]
    public int Id { get; init; }
}
