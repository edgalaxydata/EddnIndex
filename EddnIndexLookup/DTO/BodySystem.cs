using System;
using System.Collections.Generic;
using System.Text;

namespace EddnIndexLookup.DTO;

/// <summary>
/// System details for a given body
/// </summary>
public class BodySystem
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
}
