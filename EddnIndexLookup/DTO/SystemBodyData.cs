using EddnIndexUpdate.Models;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Matched body details
/// </summary>
[DataContract]
public record class SystemBodyData : IMatchedItem, IBodyData
{
    /// <summary>
    /// Body Name
    /// </summary>
    /// <example>Rigel</example>
    [DataMember(Name = "Name")]
    public string Name { get; init; } = "";

    /// <summary>
    /// System Address (AKA ID64)
    /// </summary>
    [Range(0, 1L << 55, MaximumIsExclusive = true)]
    [DataMember(Name = "SystemAddress")]
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Sequential body id within a system
    /// </summary>
    [Range(0, 511)]
    [DataMember(Name = "BodyId")]
    public int? BodyId { get; init; }

    /// <summary>
    /// Parent heirarchy to system's root body
    /// </summary>
    /// <example>[{"Planet":4},{"Star":1},{"Null":0}]</example>
    [DataMember(Name = "Parents")]
    public List<Dictionary<string, int>>? Parents { get; init; }

    /// <summary>
    /// Body type
    /// </summary>
    /// <example>Star</example>
    [DataMember(Name = "BodyType")]
    public string? BodyType { get; init; }

    /// <summary>
    /// Body designation if known
    /// </summary>
    [DataMember(Name = "Designation")]
    public string? Designation { get; init; }

    /// <summary>
    /// Body designation type
    /// </summary>
    [DataMember(Name = "DesignationType")]
    public string? DesignationType { get; init; }

    /// <summary>
    /// Orbital Argument of Periapsis
    /// </summary>
    [DataMember(Name = "ArgOfPeriapsis")]
    public decimal? ArgOfPeriapsis { get; init; }

    /// <summary>
    /// Orbital inclination
    /// </summary>
    [DataMember(Name = "Inclination")]
    public decimal? Inclination { get; init; }

    /// <summary>
    /// Orbital Semi-Major Axis
    /// </summary>
    [DataMember(Name = "SemiMajorAxis")]
    public decimal? SemiMajorAxis { get; init; }

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
    /// Stations from events matching these details
    /// </summary>
    [DataMember(Name = "Stations")]
    public List<StationData>? Stations { get; init; }

    /// <summary>
    /// Body designation data
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public BodyDesignation? BodyDesignation { get; init; }

    /// <summary>
    /// Internal system id
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public int SystemId { get; init; }

    /// <summary>
    /// Internal body id
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public long Id { get; init; }
}

