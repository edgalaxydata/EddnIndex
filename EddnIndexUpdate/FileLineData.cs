using System.Text.Json;

namespace EddnIndexUpdate
{
    public struct FileLineData
    {
        private Dictionary<int, Models.System> _navRouteSystems;
        private Dictionary<int, Models.SignalInfo> _signals;
        private Dictionary<int, Models.BodySignalInfo> _bodySignals;
        private Dictionary<int, (string Name, decimal? innerRadius, decimal? outerRadius)> _ringData;
        private Dictionary<int, (Models.Body body, short? smadiff, short? aopdiff, short? incdiff)> _subBodies;
        private Dictionary<(string Name, JsonTokenType TokenType), int> _messageKeyCounts;

        public Models.File File { get; set; }
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
        public Models.System? System { get; set; }
        public Models.Body? Body { get; set; }
        public short? SemiMajorAxisError { get; set; }
        public short? ArgOfPeriapsisError { get; set; }
        public short? InclinationError { get; set; }
        public Models.Station? Station { get; set; }
        public Models.SoftwareInfo? Software { get; set; }
        public Models.GameVersionInfo? GameVersionInfo { get; set; }
        public Dictionary<int, (string Name, decimal? innerRadius, decimal? outerRadius)> RingData => _ringData ??= [];
        public Dictionary<int, (Models.Body body, short? smaerror, short? aoperror, short? incerror)> SubBodies => _subBodies ??= [];
        public Dictionary<int, Models.System> NavRouteSystems => _navRouteSystems ??= [];
        public Dictionary<int, Models.SignalInfo> Signals => _signals ??= [];
        public Dictionary<int, Models.BodySignalInfo> BodySignals => _bodySignals ??= [];
        public Dictionary<(string Name, JsonTokenType TokenType), int> MessageKeyCounts => _messageKeyCounts ??= [];

        public void Clear(Models.File file, int lineNo, int lineLength)
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
            RingData.Clear();
            SubBodies.Clear();
            NavRouteSystems.Clear();
            Signals.Clear();
            BodySignals.Clear();
        }
    }
}
