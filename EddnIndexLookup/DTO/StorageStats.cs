using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Storage statistics
/// </summary>
[DataContract]
public class StorageStats
{
    /// <summary>
    /// Table statistics
    /// </summary>
    [DataMember(Name = "Tables")]
    public Dictionary<string, TableInfo> Tables { get; init; } = [];

    /// <summary>
    /// Storage used by dumps
    /// </summary>
    [DataMember(Name = "DumpUsages")]
    public Dictionary<string, DumpDirectoryUsage> DumpUsages { get; init; } = [];
}
