using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddnIndexUpdate.Models
{
    public record class SignalInfoSet
    {
        public int Id { get; set; }
        public int FirstSignalId { get; set; }
        public int LastSignalId { get; set; }
        public int SignalCount { get; set; }
        public required string SignalSetJson { get; set; }

        public List<SignalInfoSetItem> SignalSetItems { get; set; } = [];
    }
}
