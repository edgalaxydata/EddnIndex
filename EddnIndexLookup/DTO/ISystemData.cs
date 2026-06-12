namespace EddnIndexLookup.DTO;

/// <summary>
/// System details
/// </summary>
public interface ISystemData
{
    /// <summary>
    /// Name of system
    /// </summary>
    string Name { get; init; }

    /// <summary>
    /// Procedurally generated name of system where available
    /// </summary>
    string? PGName { get; init; }

    /// <summary>
    /// Unique identifier for system from event
    /// </summary>
    long? SystemAddress { get; init; }

    /// <summary>
    /// Unique identifier based on system name
    /// </summary>
    long? NameSystemAddress { get; init; }

    /// <summary>
    /// Heliocentric galactic rectangular coordinates of system in lightyears
    /// </summary>
    Coords? Coords { get; init; }

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
    /// Internal system id
    /// </summary>
    int Id { get; init; }
}
