using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddnIndexUpdate.Models
{
    public record class GameVersionDate : IHasId<int>
    {
        public int Id { get; set; }
        public string? Season { get; set; }
        public required string Version { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime? UpdateStartTime { get; set; }
        public DateTime? UpdateEndTime { get; set; }
        public string? Description { get; set; }
        public bool? IsAlphaOrBeta { get; set; }
    }
}
