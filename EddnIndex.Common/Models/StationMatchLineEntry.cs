namespace EddnIndex.Common.Models;

public record struct StationMatchLineEntry(FileInfo File, FileLineStation Station, FileLineInfo Info, FileLineBody? Body);