namespace EddnIndex.Common.Models;

public class SignalInfoSetItem : IHasId<int>
{
    public int Id { get; set; }
    public int SignalInfoSetId { get; set; }
    public int SignalInfoId { get; set; }
    public int Count { get; set; }
    public int? SystemId { get; set; }

    public SignalInfo? Signal { get; set; }
    public SystemInfo? System { get; set; }
}
