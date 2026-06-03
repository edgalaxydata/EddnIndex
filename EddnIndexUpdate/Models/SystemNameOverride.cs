using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddnIndexUpdate.Models;

public record class SystemNameOverride : IHasId<int>
{
    public int Id { get; init; }
    public long SystemAddress { get; init; }
    public required string Name { get; init; }
    public decimal? X { get; init; }
    public decimal? Y { get; init; }
    public decimal? Z { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
}
