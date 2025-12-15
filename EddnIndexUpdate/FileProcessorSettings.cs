using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddnIndexUpdate
{
    public class FileProcessorSettings
    {
        public class OverridesURISettings<T>
            where T : notnull, new()
        {
            public string? URI { get; set; }
            public string? Filename { get; set; }
            public T Fields { get; set; } = new T();
        }

        public class BodyOverridesCsvFieldSettings
        {
            public string SystemName { get; set; } = "System Name";
            public string BodyDesignation { get; set; } = "Canonical Body Name";
            public string BodyName { get; set; } = "Body Name";
            public string SystemAddress { get; set; } = "SystemAddress";
            public string BodyID { get; set; } = "BodyID";
            public string SinceVersion { get; set; } = "Since Version";
            public string UntilVersion { get; set; } = "Until Version";
            public string IsStar { get; set; } = "Is Star";
            public string BodyType { get; set; } = "";
            public string ArgOfPeriapsis { get; set; } = "Arg of Periapsis";
            public string Inclination { get; set; } = "Inclination";
        }

        public class SystemRenamesCsvFieldSettings
        {
            public string PreviousSystemName { get; set; } = "Previous System Name";
            public string SystemName { get; set; } = "System Name";
            public string SystemAddress { get; set; } = "System Address";
            public string RenameDate { get; set; } = "Approx Rename Date";
        }

        public class SystemOverridesCsvFieldSettings
        {
            public string SystemName { get; set; } = "System Name";
            public string SystemAddress { get; set; } = "SystemAddress";
            public string X { get; set; } = "X";
            public string Y { get; set; } = "Y";
            public string Z { get; set; } = "Z";
        }

        public class SystemOverridesJsonFieldSettings
        {
            public string SystemName { get; set; } = "$.name";
            public string SystemAddress { get; set; } = "$.id";
            public string X { get; set; } = "$.coords[0]";
            public string Y { get; set; } = "$.coords[1]";
            public string Z { get; set; } = "$.coords[2]";
        }

        public class GameVersionDatesCsvFieldSettings
        {
            public string UpdateTime { get; set; } = "UpdateTime";
            public string UpdateStartTime { get; set; } = "UpdateStartTime";
            public string UpdateEndTime { get; set; } = "UpdateEndTime";
            public string IsAlphaOrBeta { get; set; } = "IsAlphaOrBeta";
            public string VersionPrefix { get; set; } = "Version_";
            public string Version_1_0 { get; set; } = "-";
            public string Version_Horizons { get; set; } = "-";
            public string Version_Odyssey { get; set; } = "-";
            public string Season { get; set; } = "-";
            public string Version { get; set; } = "-";
        }

        public OverridesURISettings<BodyOverridesCsvFieldSettings> BodyOverridesCsv { get; set; } = new();

        public OverridesURISettings<SystemRenamesCsvFieldSettings> SystemRenamesCsv { get; set; } = new();

        public OverridesURISettings<SystemOverridesCsvFieldSettings> SystemOverridesCsv { get; set; } = new();

        public OverridesURISettings<SystemOverridesJsonFieldSettings> SystemOverridesJson { get; set; } = new();

        public OverridesURISettings<GameVersionDatesCsvFieldSettings> GameVersionDatesCsv { get; set; } = new();

        public string BodyOverridesFile { get; set; } = "body-name-overrides.jsonl";
        public string SystemOverridesFile { get; set; } = "system-name-overrides.jsonl";
        public string GameVersionDatesFile { get; set; } = "game-version-dates.jsonl";
        public string MessageTypesFile { get; set; } = "msgtypes.txt";
        public string BaseDir { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
        public string? IndexedDir { get; set; }
        public bool? BreakOnBadData { get; set; }
        public bool? ExitOnBadData { get; set; }
    }
}
