using System.ComponentModel.DataAnnotations;
using EddnIndex.Common.Models;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Matched body details
/// </summary>
public interface IBodyData
{
    /// <summary>
    /// Body Name
    /// </summary>
    /// <example>Rigel</example>
    string Name { get; init; }

    /// <summary>
    /// System Address (AKA ID64)
    /// </summary>
    [Range(0, 1L << 55, MaximumIsExclusive = true)]
    long? SystemAddress { get; init; }

    /// <summary>
    /// Sequential body id within a system
    /// </summary>
    [Range(0, 511)]
    int? BodyId { get; init; }

    /// <summary>
    /// Parent heirarchy to system's root body
    /// </summary>
    /// <example>[{"Planet":4},{"Star":1},{"Null":0}]</example>
    List<Dictionary<string, int>>? Parents { get; init; }

    /// <summary>
    /// Body type
    /// </summary>
    /// <example>Star</example>
    string? BodyType { get; init; }

    /// <summary>
    /// Body designation if known
    /// </summary>
    string? Designation { get; init; }

    /// <summary>
    /// Body designation type
    /// </summary>
    string? DesignationType { get; init; }

    /// <summary>
    /// Orbital Argument of Periapsis
    /// </summary>
    decimal? ArgOfPeriapsis { get; init; }

    /// <summary>
    /// Orbital inclination
    /// </summary>
    decimal? Inclination { get; init; }

    /// <summary>
    /// Orbital Semi-Major Axis
    /// </summary>
    decimal? SemiMajorAxis { get; init; }

    /// <summary>
    /// Set to true if item details were determined to be invalid
    /// </summary>
    bool? IsRejected { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date from which details are valid
    /// </summary>
    DateTime? ValidFrom { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date until which details were valid
    /// </summary>
    DateTime? ValidTo { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was first seen with these details
    /// </summary>
    DateTime? FirstSeen { get; init; }

    /// <summary>
    /// GatewayTimestamp when item was last seen with these details
    /// </summary>
    DateTime? LastSeen { get; init; }

    /// <summary>
    /// Body designation data
    /// </summary>
    BodyDesignation? BodyDesignation { get; init; }

    /// <summary>
    /// Internal system id
    /// </summary>
    int SystemId { get; init; }

    /// <summary>
    /// Internal body id
    /// </summary>
    long Id { get; init; }
}
