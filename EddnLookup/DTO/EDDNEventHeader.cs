using System;
using System.Collections.Generic;
using System.Text;

namespace EddnLookup.DTO;

/// <summary>
/// EDDN event header
/// </summary>
public class EDDNEventHeader
{
    /// <summary>
    /// UTC Timestamp when message was received by EDDN gateway
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("gatewayTimestamp")]
    [Newtonsoft.Json.JsonProperty("gatewayTimestamp")]
    public DateTimeOffset? GatewayTimestamp { get; init; }

    /// <summary>
    /// Game Build from Fileheader or LoadGame journal event
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("gamebuild")]
    [Newtonsoft.Json.JsonProperty("gamebuild")]
    public string? GameBuild { get; init; }

    /// <summary>
    /// Game Version from Fileheader or LoadGame journal event
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("gameversion")]
    [Newtonsoft.Json.JsonProperty("gameversion")]
    public string? GameVersion { get; init; }

    /// <summary>
    /// Submitting software name
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("softwareName")]
    [Newtonsoft.Json.JsonProperty("softwareName")]
    public string? SoftwareName { get; init; }

    /// <summary>
    /// Submitting software version
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("softwareVersion")]
    [Newtonsoft.Json.JsonProperty("softwareVersion")]
    public string? SoftwareVersion { get; init; }

    /// <summary>
    /// Opaque short-lifetime identifier linking events submitted within a time window by a single uploader.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("uploaderID")]
    [Newtonsoft.Json.JsonProperty("uploaderID")]
    public string? UploaderID { get; init; }

    /// <summary>
    /// Extra JSON properties
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    [Newtonsoft.Json.JsonExtensionData]
    public Dictionary<string, object?> ExtraData { get; init; } = [];
}
