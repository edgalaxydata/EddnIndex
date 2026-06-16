namespace EddnIndex.Common.Models;

public interface IHasId<T>
    where T : unmanaged
{
    T Id { get; }
}
