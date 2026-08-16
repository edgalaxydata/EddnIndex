using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// EDDN event header
/// </summary>
[DataContract]
public class EDDNEventHeader
{
    /// <summary>
    /// UTC Timestamp when message was received by EDDN gateway
    /// </summary>
    [DataMember(Name = "gatewayTimestamp")]
    public DateTime? GatewayTimestamp { get; init; }

    /// <summary>
    /// Game Build from Fileheader or LoadGame journal event
    /// </summary>
    [DataMember(Name = "gamebuild")]
    public string? GameBuild { get; init; }

    /// <summary>
    /// Game Version from Fileheader or LoadGame journal event
    /// </summary>
    [DataMember(Name = "gameversion")]
    public string? GameVersion { get; init; }

    /// <summary>
    /// Submitting software name
    /// </summary>
    [DataMember(Name = "softwareName")]
    public string? SoftwareName { get; init; }

    /// <summary>
    /// Submitting software version
    /// </summary>
    [DataMember(Name = "softwareVersion")]
    public string? SoftwareVersion { get; init; }

    /// <summary>
    /// Opaque short-lifetime identifier linking events submitted within a time window by a single uploader.
    /// </summary>
    [DataMember(Name = "uploaderID")]
    public string? UploaderID { get; init; }

    /// <summary>
    /// Extra JSON properties
    /// </summary>
    [System.Text.Json.Serialization.JsonExtensionData]
    [Newtonsoft.Json.JsonExtensionData]
    public Dictionary<string, object?> ExtraData { get; init; } = [];
}
