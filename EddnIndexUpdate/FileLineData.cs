using System.Text.Json;
using Models = EddnIndex.Common.Models;

namespace EddnIndexUpdate;

public class FileLineData
{
    public required Models.FileInfo File { get; set; }
    public int LineNo { get; set; }
    public int LineLength { get; set; }

    public string? Schema { get; set; }
    public string? EventType { get; set; }

    public string? GameVersion { get; set; }
    public string? GameBuild { get; set; }
    public bool? IsHorizons { get; set; }
    public bool? IsOdyssey { get; set; }

    public bool IsBad { get; set; }

    public DateTime? Timestamp { get; set; }
    public DateTime? GatewayTimestamp { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public short? SemiMajorAxisError { get; set; }
    public short? ArgOfPeriapsisError { get; set; }
    public short? InclinationError { get; set; }

    public string? SoftwareName { get; set; }
    public string? SoftwareVersion { get; set; }

    public string? StationName { get; set; }
    public long? MarketId { get; set; }
    public string? StationType { get; set; }

    public string? SystemName { get; set; }
    public long? SystemAddress { get; set; }
    public decimal? X { get; set; }
    public decimal? Y { get; set; }
    public decimal? Z { get; set; }

    public string? BodyName { get; set; }
    public string? BodyType { get; set; }
    public int? BodyId { get; set; }
    public string? ParentsJson { get; set; }
    public decimal? SemiMajorAxis { get; set; }
    public decimal? ArgOfPeriapsis { get; set; }
    public decimal? Inclination { get; set; }

    public Models.SystemInfo? System { get; set; }
    public Models.BodyInfo? Body { get; set; }
    public Models.StationInfo? Station { get; set; }
    public Models.SoftwareInfo? Software { get; set; }
    public Models.GameVersionInfo? GameVersionInfo { get; set; }
    public Models.SchemaEventInfo? SchemaEvent { get; set; }

    public List<string> Errors { get; } = [];
    public Dictionary<int, (string CodexName, int? Count, string? Category, string? SubCategory, string? Region, long? EntryId)> BodySignalInfo { get; } = [];
    public Dictionary<int, (string Name, decimal? innerRadius, decimal? outerRadius)> RingData { get; } = [];
    public Dictionary<int, (string SystemName, long? SystemAddress, decimal? X, decimal? Y, decimal? Z)> NavRouteSystemInfo { get; } = [];
    public Dictionary<int, (string Name, string? Type, bool? IsStation)> SignalInfo { get; } = [];

    public Dictionary<int, (Models.BodyInfo body, short? smaerror, short? aoperror, short? incerror)> SubBodies { get; } = [];
    public Dictionary<int, Models.SystemInfo> NavRouteSystems { get; } = [];
    public Dictionary<int, Models.SignalInfo> Signals { get; } = [];
    public Dictionary<int, Models.BodySignalInfo> BodySignals { get; } = [];

    public Dictionary<(string Name, JsonTokenType TokenType), int> MessageKeyCounts { get; } = [];

    public void Clear(Models.FileInfo file, int lineNo, int lineLength)
    {
        File = file;
        LineNo = lineNo;
        LineLength = lineLength;

        Schema = null;
        EventType = null;

        GameVersion = null;
        GameBuild = null;
        IsHorizons = null;
        IsOdyssey = null;

        IsBad = false;

        Timestamp = null;
        GatewayTimestamp = null;

        Latitude = null;
        Longitude = null;

        SemiMajorAxisError = null;
        ArgOfPeriapsisError = null;
        InclinationError = null;

        SoftwareName = null;
        SoftwareVersion = null;

        StationName = null;
        MarketId = null;
        StationType = null;

        SystemName = null;
        SystemAddress = null;
        X = null;
        Y = null;
        Z = null;

        BodyName = null;
        BodyId = null;
        BodyType = null;
        ParentsJson = null;
        SemiMajorAxis = null;
        ArgOfPeriapsis = null;
        Inclination = null;

        System = null;
        Body = null;
        Station = null;
        Software = null;
        GameVersionInfo = null;
        SchemaEvent = null;

        Errors.Clear();
        BodySignalInfo.Clear();
        RingData.Clear();
        NavRouteSystemInfo.Clear();
        SignalInfo.Clear();

        SubBodies.Clear();
        NavRouteSystems.Clear();
        Signals.Clear();
        BodySignals.Clear();
    }
}
