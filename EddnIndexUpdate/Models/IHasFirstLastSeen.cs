namespace EddnIndexUpdate.Models;

public interface IHasFirstLastSeen
{
    DateTime? FirstSeen { get; }
    DateTime? LastSeen { get; }
}
