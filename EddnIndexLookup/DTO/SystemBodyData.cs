using System.ComponentModel.DataAnnotations;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Matched body details
/// </summary>
public record class SystemBodyData : IMatchedItem, IBodyData
{
    /// <summary>
    /// Body Name
    /// </summary>
    /// <example>Rigel</example>
    public string Name { get; init; } = "";

    /// <summary>
    /// System Address (AKA ID64)
    /// </summary>
    [Range(0, 1L << 55, MaximumIsExclusive = true)]
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Sequential body id within a system
    /// </summary>
    [Range(0, 511)]
    public int? BodyId { get; init; }

    /// <summary>
    /// Parent heirarchy to system's root body
    /// </summary>
    /// <example>[{"Planet":4},{"Star":1},{"Null":0}]</example>
    public List<Dictionary<string, int>>? Parents { get; init; }

    /// <summary>
    /// Body type
    /// </summary>
    /// <example>Star</example>
    public string? BodyType { get; init; }

    /// <summary>
    /// Body designation if known
    /// </summary>
    public string? Designation { get; init; }

    /// <summary>
    /// Body designation type
    /// </summary>
    public string? DesignationType { get; init; }

    /// <summary>
    /// Orbital Argument of Periapsis
    /// </summary>
    public decimal? ArgOfPeriapsis { get; init; }

    /// <summary>
    /// Orbital inclination
    /// </summary>
    public decimal? Inclination { get; init; }

    /// <summary>
    /// Orbital Semi-Major Axis
    /// </summary>
    public decimal? SemiMajorAxis { get; init; }

    /// <summary>
    /// Set to true if item details were determined to be invalid
    /// </summary>
    public bool? IsRejected { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date from which details are valid
    /// </summary>
    public DateTime? ValidFrom { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date until which details were valid
    /// </summary>
    public DateTime? ValidTo { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was first seen with these details
    /// </summary>
    public DateTime? FirstSeen { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was last seen with these details
    /// </summary>
    public DateTime? LastSeen { get; init; }

    /// <summary>
    /// Number of events matching these details
    /// </summary>
    public int? MatchCount { get; init; }

    /// <summary>
    /// Possibly filtered list of events matching these details
    /// </summary>
    public List<MatchEntry>? Matches { get; init; }

    /// <summary>
    /// Internal system id
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int SystemId { get; init; }

    /// <summary>
    /// Internal body id
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public long Id { get; init; }
}

