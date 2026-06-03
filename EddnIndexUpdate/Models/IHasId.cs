using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EddnIndexUpdate.Models;

public interface IHasId<T>
    where T : unmanaged
{
    T Id { get; }
}
