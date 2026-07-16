using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// System details for a given body
/// </summary>
[DataContract]
public class SignalSystem : ISystemData
{
    /// <summary>
    /// Name of system
    /// </summary>
    [Required]
    [DataMember(Name = "Name", IsRequired = true)]
    public string Name { get; init; } = "";

    /// <summary>
    /// Procedurally generated name of system where available
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public string? PGName { get; init; }

    /// <summary>
    /// Unique identifier for system from event
    /// </summary>
    [DataMember(Name = "SystemAddress")]
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Unique identifier based on system name
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public long? NameSystemAddress { get; init; }

    /// <summary>
    /// Heliocentric galactic rectangular coordinates of system in lightyears
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public Coords? Coords { get; init; }

    /// <summary>
    /// Set to true if item details were determined to be invalid
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public bool? IsRejected { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date from which details are valid
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public DateTime? ValidFrom { get; init; }

    /// <summary>
    /// Set if the system was renamed or reassigned; Date until which details were valid
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
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
    /// Internal system id
    /// </summary>
    [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
    public int Id { get; init; }
}
