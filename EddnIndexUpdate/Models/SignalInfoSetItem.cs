namespace EddnIndexUpdate.Models
{
    public class SignalInfoSetItem
    {
        public int Id { get; set; }
        public int SignalInfoSetId { get; set; }
        public int SignalInfoId { get; set; }
        public int Count { get; set; }

        public SignalInfo? Signal { get; set; }
    }
}
