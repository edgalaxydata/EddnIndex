namespace EddnIndexUpdate;

public class BadDataException(string? message, object? extradata) : Exception(message)
{
    public object? ExtraData { get; } = extradata;
}
