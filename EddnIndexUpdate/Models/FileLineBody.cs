namespace EddnIndexUpdate.Models;

public record class FileLineBody
{
    public int FileId { get; init; }
    public int LineNo { get; init; }
    public int EntryNum { get; init; }
    public long BodyId { get; init; }
    public DateTime? GatewayTimestamp { get; init; }
    public short? ArgOfPeriapsisError { get; init; }
    public short? InclinationError { get; init; }
    public short? SemiMajorAxisError { get; init; }

    public BodyInfo? Body { get; init; }
}
