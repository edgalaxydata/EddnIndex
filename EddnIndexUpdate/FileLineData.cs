using System.Text.Json;

namespace EddnIndexUpdate;

public struct FileLineData
{
    public Models.FileInfo File { get; set; }
    public string? Schema { get; set; }
    public string? EventType { get; set; }
    public string? GameVersion { get; set; }
    public string? GameBuild { get; set; }
    public bool? IsHorizons { get; set; }
    public bool? IsOdyssey { get; set; }
    public int LineNo { get; set; }
    public int LineLength { get; set; }
    public bool IsBad { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateTime? Timestamp { get; set; }
    public DateTime? GatewayTimestamp { get; set; }
    public Models.SystemInfo? System { get; set; }
    public Models.BodyInfo? Body { get; set; }
    public short? SemiMajorAxisError { get; set; }
    public short? ArgOfPeriapsisError { get; set; }
    public short? InclinationError { get; set; }
    public Models.StationInfo? Station { get; set; }
    public Models.SoftwareInfo? Software { get; set; }
    public Models.GameVersionInfo? GameVersionInfo { get; set; }
    public Models.SchemaEventInfo? SchemaEvent { get; set; }
    public Dictionary<int, (string Name, decimal? innerRadius, decimal? outerRadius)> RingData => field ??= [];
    public Dictionary<int, (Models.BodyInfo body, short? smaerror, short? aoperror, short? incerror)> SubBodies => field ??= [];
    public Dictionary<int, Models.SystemInfo> NavRouteSystems => field ??= [];
    public Dictionary<int, Models.SignalInfo> Signals => field ??= [];
    public Dictionary<int, Models.BodySignalInfo> BodySignals => field ??= [];
    public Dictionary<(string Name, JsonTokenType TokenType), int> MessageKeyCounts => field ??= [];

    public void Clear(Models.FileInfo file, int lineNo, int lineLength)
    {
        File = file;
        LineNo = lineNo;
        LineLength = lineLength;
        IsBad = false;
        Schema = null;
        EventType = null;
        GameVersion = null;
        GameBuild = null;
        IsHorizons = null;
        IsOdyssey = null;
        Timestamp = null;
        GatewayTimestamp = null;
        Latitude = null;
        Longitude = null;
        System = null;
        Body = null;
        Station = null;
        Software = null;
        GameVersionInfo = null;
        SchemaEvent = null;
        RingData.Clear();
        SubBodies.Clear();
        NavRouteSystems.Clear();
        Signals.Clear();
        BodySignals.Clear();
    }
}
