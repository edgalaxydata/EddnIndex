namespace EddnIndexUpdate.Models;

public interface IHasId<T>
    where T : unmanaged
{
    T Id { get; }
}
