using System;
using System.Collections.Generic;
using System.Text;

namespace EddnIndexLookup.DTO;

/// <summary>
/// EDDN Event
/// </summary>
public class EDDNEvent
{
    /// <summary>
    /// Schema URL (e.g. https://eddn.edcd.io/schemas/journal/1)
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("$schemaRef")]
    [Newtonsoft.Json.JsonProperty("$schemaRef")]
    public required string SchemaRef { get; init; }

    /// <summary>
    /// EDDN event header
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("header")]
    [Newtonsoft.Json.JsonProperty("header")]
    public required EDDNEventHeader Header { get; init; }

    /// <summary>
    /// EDDN event body
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    [Newtonsoft.Json.JsonProperty("message")]
    public required EDDNEventMessage Message { get; init; }
}
