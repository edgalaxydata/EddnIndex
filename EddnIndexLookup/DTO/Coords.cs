using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Heliocentric System Coordinates
/// </summary>
/// <param name="X">Heliocentric X coordinate (positive towards NGC 2866 from Sol) </param>
/// <param name="Y">Heliocentric Y coordinate (positive towards NGC 4721 from Sol)</param>
/// <param name="Z">Heliocentric Z coordinate (positive towards Sagittarius A* from Sol)</param>
[DataContract]
public record class Coords(
    [property: DataMember(Name = "X")] decimal X,
    [property: DataMember(Name = "Y")] decimal Y,
    [property: DataMember(Name = "Z")] decimal Z
);
