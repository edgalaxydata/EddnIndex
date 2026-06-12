using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Station details
/// </summary>
[DataContract]
public record class OldStationData
{
    /// <summary>
    /// Unique ID of station
    /// </summary>
    [DataMember(Name = "MarketId")]
    public long? MarketId { get; init; }

    /// <summary>
    /// Name of station
    /// </summary>
    [DataMember(Name = "Name")]
    public string? Name { get; init; }

    /// <summary>
    /// Type of station
    /// </summary>
    [DataMember(Name = "StationType")]
    public string? StationType { get; init; }

    /// <summary>
    /// System name for fixed stations
    /// </summary>
    [DataMember(Name = "SystemName")]
    public string? SystemName { get; init; }

    /// <summary>
    /// System Address for fixed stations
    /// </summary>
    [DataMember(Name = "SystemAddress")]
    public long? SystemAddress { get; init; }

    /// <summary>
    /// Name of body the SOI of which the station is fixed in
    /// </summary>
    [DataMember(Name = "BodyName")]
    public string? BodyName { get; init; }

    /// <summary>
    /// Latitude for ground settlements
    /// </summary>
    [DataMember(Name = "Latitude")]
    public decimal? Latitude { get; init; }

    /// <summary>
    /// Longitude for ground settlements
    /// </summary>
    [DataMember(Name = "Longitude")]
    public decimal? Longitude { get; init; }

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

    /// <summary>
    /// Convert to old stations.php station object
    /// </summary>
    /// <param name="station">Station data</param>
    /// <returns>stations.php station object</returns>
    public static OldStationData From(StationData station)
    {
        return new OldStationData
        {
            MarketId = station.MarketId,
            SystemAddress = station.SystemAddress,
            BodyName = station.BodyName,
            FirstSeen = station.FirstSeen,
            IsRejected = station.IsRejected,
            LastSeen = station.LastSeen,
            Latitude = station.Latitude,
            Longitude = station.Longitude,
            MatchCount = station.MatchCount,
            Matches = station.Matches,
            Name = station.StationName,
            StationType = station.StationType,
            SystemName = station.SystemName,
            ValidFrom = station.ValidFrom,
            ValidTo = station.ValidTo
        };
    }
}
