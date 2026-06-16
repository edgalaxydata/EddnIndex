namespace EddnIndex.Common.Models;

public interface IHasFirstLastSeen
{
    DateTime? FirstSeen { get; }
    DateTime? LastSeen { get; }
}
