using System.Runtime.Serialization;

namespace EddnIndexLookup.DTO;

/// <summary>
/// Statistics for a dump directory
/// </summary>
[DataContract]
public record class DumpDirectoryUsage
{
    /// <summary>
    /// Directory name
    /// </summary>
    [DataMember(Name = "DirectoryName", IsRequired = true)]
    public required string DirectoryName { get; init; }

    /// <summary>
    /// Total size of data in directory
    /// </summary>
    [DataMember(Name = "DataSize")]
    public long DataSize { get; init; }

    /// <summary>
    /// Number of files in directory
    /// </summary>
    [DataMember(Name = "FileCount")]
    public int FileCount { get; init; }
}
