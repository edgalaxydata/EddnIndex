namespace EddnIndex.Common.Models;

public record struct BodyMatchLineEntry(FileInfo File, FileLineBody Body, FileLineInfo Info, FileLineStation? Station);
