using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// System summary
/// </summary>
[DataContract]
public class SystemGapData
{
    /// <summary>
    /// Unique identifier for system (AKA ID64)
    /// </summary>
    public long SystemAddress { get; init; }

    /// <summary>
    /// Procedural boxel prefix without sequence number
    /// </summary>
    public required string NamePrefix { get; init; }

    /// <summary>
    /// Procedural sequence number within boxel
    /// </summary>
    public int SequenceNumber { get; init; }

    /// <summary>
    /// System first seen through EDDN
    /// </summary>
    public DateTime? FirstSeen { get; init; }

    /// <summary>
    /// System last seen through EDDN
    /// </summary>
    public DateTime? LastSeen { get; init; }
}
