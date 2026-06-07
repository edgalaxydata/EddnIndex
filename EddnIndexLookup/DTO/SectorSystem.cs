using System.ComponentModel.DataAnnotations;

namespace EddnIndexLookup.DTO
{
    /// <summary>
    /// System details
    /// </summary>
    public class SectorSystem : ISystemData
    {
        /// <summary>
        /// Name of system
        /// </summary>
        [Required]
        public string Name { get; init; } = "";

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
        /// Internal system id
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int Id { get; init; }
    }
}
