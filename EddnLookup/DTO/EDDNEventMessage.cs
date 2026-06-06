using System;
using System.Collections.Generic;
using System.Text;

namespace EddnLookup.DTO
{
    /// <summary>
    /// EDDN event body
    /// </summary>
    public class EDDNEventMessage
    {
        /// <summary>
        /// Journal event type
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("event")]
        [Newtonsoft.Json.JsonProperty("event")]
        public string? Event { get; init; }

        /// <summary>
        /// True if game mode was Horizons or later (i.e. planetary landings are possible)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("horizons")]
        [Newtonsoft.Json.JsonProperty("horizons")]
        public bool? Horizons { get; init; }

        /// <summary>
        /// True if game mode was Odyssey or later (i.e. on-foot actions are possible)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("odyssey")]
        [Newtonsoft.Json.JsonProperty("odyssey")]
        public bool? Odyssey { get; init; }

        /// <summary>
        /// UTC Event timestamp from event source (e.g. journal)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
        [Newtonsoft.Json.JsonProperty("timestamp")]
        public DateTime? Timestamp { get; init; }
    }
}
