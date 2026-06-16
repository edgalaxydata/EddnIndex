using EddnIndex.Common.Models;
using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO
{
    /// <summary>
    /// Data on a signal in FSSSignalsDiscovered
    /// </summary>
    public record class SignalData : IMatchedItem
    {
        /// <summary>
        /// Possibly localized name of signal
        /// </summary>
        public required string SignalName { get; init; }
        
        /// <summary>
        /// Type of signal
        /// </summary>
        public string? SignalType { get; init; }

        /// <summary>
        /// Set to true if signal is a station or fleet carrier
        /// </summary>
        public bool? IsStation { get; init; }

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

        [IgnoreDataMember, System.Text.Json.Serialization.JsonIgnore, Newtonsoft.Json.JsonIgnore]
        internal int Id { get; init; }
    }
}
