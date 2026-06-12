using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EddnIndexLookup.DTO;

/// <summary>
/// EDDN Event
/// </summary>
[DataContract]
public class EDDNEvent
{
    /// <summary>
    /// Schema URL (e.g. https://eddn.edcd.io/schemas/journal/1)
    /// </summary>
    [DataMember(Name = "$schemaRef")]
    public required string SchemaRef { get; init; }

    /// <summary>
    /// EDDN event header
    /// </summary>
    [DataMember(Name = "header")]
    public required EDDNEventHeader Header { get; init; }

    /// <summary>
    /// EDDN event body
    /// </summary>
    [DataMember(Name = "message")]
    public required EDDNEventMessage Message { get; init; }
}
