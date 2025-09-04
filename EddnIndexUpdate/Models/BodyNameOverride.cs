using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddnIndexUpdate.Models
{
    public record class BodyNameOverride
    {
        public int Id { get; init; }
        public long SystemAddress { get; init; }
        public required string SystemName { get; init; }
        public int BodyID { get; init; }
        public required string BodyName { get; init; }
        public required string BodyDesignation { get; init; }
        public string? BodyType { get; init; }
        public decimal? ArgOfPeriapsis { get; init; }
        public decimal? Inclination { get; init; }
        public string? SinceVersion { get; init; }
        public string? UntilVersion { get; init; }
        public DateTime? ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }

        public bool? ArgOfPeriapsisEquals(decimal? other)
        {
            if (this.ArgOfPeriapsis is decimal thisVal && other is decimal otherVal)
            {
                return Math.Abs((thisVal + 360) % 360 - (otherVal + 360) % 360) < 1;
            }

            return null;
        }

        public bool? InclinationEquals(decimal? other)
        {
            if (this.Inclination is decimal thisVal && other is decimal otherVal)
            {
                return Math.Abs((thisVal + 360) % 360 - (otherVal + 360) % 360) < 1;
            }

            return null;
        }
    }
}
