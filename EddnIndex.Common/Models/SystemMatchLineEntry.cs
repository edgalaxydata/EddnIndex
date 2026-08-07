namespace EddnIndex.Common.Models;

public record struct SystemMatchLineEntry(FileInfo File, FileLineInfo Info, FileLineBody? Body, FileLineStation? Station);