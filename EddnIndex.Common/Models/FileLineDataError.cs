namespace EddnIndex.Common.Models;

public record class FileLineDataError
{
    public int FileId { get; init; }
    public int LineNo { get; init; }
    public int ErrorIndex { get; init; }
    public required string ErrorMessage { get; init; }
}
