namespace EddnIndex.Common.Models;

public record class SignalInfoSet : IHasId<int>
{
    public int Id { get; set; }
    public int FirstSignalId { get; set; }
    public int LastSignalId { get; set; }
    public int SignalCount { get; set; }
    public int? SystemId { get; set; }
    public required string SignalSetJson { get; set; }

    public List<SignalInfoSetItem> SignalSetItems { get; set; } = [];
    public SystemInfo? System { get; set; }
}
