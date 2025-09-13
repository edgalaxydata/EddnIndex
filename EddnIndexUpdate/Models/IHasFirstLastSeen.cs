using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddnIndexUpdate.Models
{
    public interface IHasFirstLastSeen
    {
        DateTime? FirstSeen { get; }
        DateTime? LastSeen { get; }
    }
}
