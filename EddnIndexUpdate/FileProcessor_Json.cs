using System.Buffers;
using System.Text;
using System.Text.Json;
using EddnIndex.Common;

namespace EddnIndexUpdate;

public partial class FileProcessor
{
    private bool TryProcessLineHeader(ref Utf8JsonReader reader, FileLineData data)
    {
        string? softwareName = null;
        string? softwareVersion = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 1) continue;
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 1) break;

            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 2)
            {
                string? name = reader.GetString();

                while (reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.Comment)
                {
                    Assert(reader.Read());
                }

                switch ((name, reader.TokenType))
                {
                    case ("gatewayTimestamp", JsonTokenType.String) when reader.TryGetDateTime(out var gwts):
                        data.GatewayTimestamp = gwts;
                        break;
                    case ("build", JsonTokenType.String):
                    case ("gamebuild", JsonTokenType.String):
                    case ("gameBuild", JsonTokenType.String):
                        data.GameBuild = reader.GetString();
                        break;
                    case ("gameversion", JsonTokenType.String):
                    case ("gameVersion", JsonTokenType.String):
                        data.GameVersion = reader.GetString();
                        break;
                    case ("softwareName", JsonTokenType.String):
                        softwareName = reader.GetString();
                        break;
                    case ("softwareVersion", JsonTokenType.String):
                        softwareVersion = reader.GetString();
                        break;
                    case ("uploaderID", JsonTokenType.String):
                        break;
                    case ("manuallyApproved", JsonTokenType.False or JsonTokenType.True):
                        break;
                    case ("horizons", JsonTokenType.False or JsonTokenType.True):
                    case ("odyssey", JsonTokenType.False or JsonTokenType.True):
                        break;
                    case ("messageID", _):
                        break;
                    default:
                        Fail($"Unknown header field {name}");
                        break;
                }
            }
        }

        if (softwareName?.Length > 255)
        {
            softwareName = softwareName[..255];
            data.IsBad = true;
            data.Errors.Add("Software name too long");
        }

        if (softwareVersion?.Length > 255)
        {
            softwareVersion = softwareVersion[..255];
            data.IsBad = true;
            data.Errors.Add("Software version too long");
        }

        //Assert(softwareName != null && softwareVersion != null);

        data.SoftwareName = softwareName;
        data.SoftwareVersion = softwareVersion;

        return true;
    }

    private bool TryProcessNavRoute(ref Utf8JsonReader reader, FileLineData data)
    {
        long? systemAddress = null;
        string? systemName = null;
        decimal? x = null;
        decimal? y = null;
        decimal? z = null;
        int itemnum = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
            if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
            {
                systemName = null;
                systemAddress = null;
                x = null;
                y = null;
                z = null;
                itemnum++;
            }

            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
            {
                Assert(systemName != null);

                data.NavRouteSystemInfo[itemnum] = (systemName, systemAddress, x, y, z);
            }

            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 4)
            {
                string? propname = reader.GetString();

                while (reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.Comment)
                {
                    Assert(reader.Read());
                }

                switch ((propname, reader.TokenType))
                {
                    case ("SystemAddress", JsonTokenType.Number) when reader.TryGetInt64(out long dv):
                        systemAddress = dv;
                        break;
                    case ("StarSystem", JsonTokenType.String):
                        systemName = reader.GetString();
                        break;
                    case ("StarPos", JsonTokenType.StartArray):
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.Number);
                        Assert(reader.TryGetDecimal(out decimal xv));
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.Number);
                        Assert(reader.TryGetDecimal(out decimal yv));
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.Number);
                        Assert(reader.TryGetDecimal(out decimal zv));
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.EndArray);
                        x = xv;
                        y = yv;
                        z = zv;
                        break;
                }
            }
        }

        return true;
    }

    private bool TryProcessSignals(ref Utf8JsonReader reader, FileLineData data)
    {
        string? name = null;
        string? type = null;
        bool? isStation = null;
        int itemnum = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
            if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
            {
                name = null;
                type = null;
                isStation = null;
                itemnum++;
            }

            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3 && name != null)
            {
                data.SignalInfo[itemnum] = (name, type, isStation);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propname = reader.GetString();

                while (reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.Comment)
                {
                    Assert(reader.Read());
                }

                switch ((propname, reader.TokenType))
                {
                    case ("SignalName", JsonTokenType.String):
                        name = reader.GetString();
                        break;
                    case ("SignalType", JsonTokenType.String):
                        type = reader.GetString();
                        break;
                    case ("IsStation", JsonTokenType.True or JsonTokenType.False):
                        isStation = reader.GetBoolean();
                        break;
                }
            }
        }

        return true;
    }

    private bool TryProcessBodySignals(ref Utf8JsonReader reader, FileLineData data)
    {
        string? type = null;
        int? count = null;
        int itemnum = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
            if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
            {
                type = null;
                count = null;
                itemnum++;
            }

            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3 && type != null)
            {
                data.BodySignalInfo[itemnum] = (type, count, null, null, null, null);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propname = reader.GetString();

                while (reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.Comment)
                {
                    Assert(reader.Read());
                }

                switch ((propname, reader.TokenType))
                {
                    case ("Count", JsonTokenType.Number) when reader.TryGetInt32(out int cnt):
                        count = cnt;
                        break;
                    case ("Type", JsonTokenType.String):
                        type = reader.GetString();
                        break;
                }
            }
        }

        return true;
    }

    private bool TryProcessRings(ref Utf8JsonReader reader, FileLineData data)
    {
        string? ringName = null;
        decimal? innerRadius = null;
        decimal? outerRadius = null;
        int itemnum = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray && reader.CurrentDepth == 2) break;
            if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 3)
            {
                ringName = null;
                itemnum++;
            }

            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3 && ringName != null)
            {
                data.RingData[itemnum] = (ringName, innerRadius, outerRadius);
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propname = reader.GetString();

                while (reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.Comment)
                {
                    Assert(reader.Read());
                }

                switch ((propname, reader.TokenType))
                {
                    case ("Name", JsonTokenType.String):
                        ringName = reader.GetString();
                        break;
                    case ("InnerRadius", JsonTokenType.Number) when reader.TryGetDecimal(out decimal dv):
                        innerRadius = dv;
                        break;
                    case ("OuterRadius", JsonTokenType.Number) when reader.TryGetDecimal(out decimal dv):
                        outerRadius = dv;
                        break;
                }
            }
        }

        return true;
    }

    private bool TryProcessLineMessage(ref Utf8JsonReader reader, ReadOnlySequence<byte> json, FileLineData data)
    {
        string? codexName = null;
        string? codexCategory = null;
        string? codexSubCategory = null;
        string? codexRegion = null;
        long? codexEntryId = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 1) break;

            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 2)
            {
                string? propname = reader.GetString();

                Assert(propname != null);

                while (reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.Comment)
                {
                    Assert(reader.Read());
                }

                data.MessageKeyCounts[(propname, reader.TokenType)] = data.MessageKeyCounts.GetValueOrDefault((propname, reader.TokenType)) + 1;

                switch ((propname, reader.TokenType))
                {
                    case ("Body" or "BodyName", JsonTokenType.String):
                        data.BodyName = reader.GetString();
                        break;
                    case ("Body" or "BodyID", JsonTokenType.Number) when reader.TryGetInt32(out int bid):
                        data.BodyId = bid;
                        break;
                    case ("BodyType", JsonTokenType.String):
                        data.BodyType = reader.GetString();
                        break;
                    case ("Parents", JsonTokenType.StartArray):
                        long pos = reader.TokenStartIndex;
                        reader.Skip();
                        var span = json.Slice(pos, reader.TokenStartIndex + 1 - pos);
                        data.ParentsJson = Encoding.UTF8.GetString(span);
                        break;
                    case ("Periapsis", JsonTokenType.Number) when reader.TryGetDecimal(out decimal dv):
                        data.ArgOfPeriapsis = dv;
                        break;
                    case ("OrbitalInclination", JsonTokenType.Number) when reader.TryGetDecimal(out decimal dv):
                        data.Inclination = dv;
                        break;
                    case ("SemiMajorAxis", JsonTokenType.Number) when reader.TryGetDecimal(out decimal dv):
                        data.SemiMajorAxis = dv;
                        break;
                    case ("StarType", JsonTokenType.String):
                        data.BodyType ??= "Star";
                        break;
                    case ("PlanetClass", JsonTokenType.String):
                        data.BodyType ??= "Planet";
                        break;
                    case ("SystemAddress", JsonTokenType.Number) when reader.TryGetInt64(out long dv):
                        data.SystemAddress = dv;
                        break;
                    case ("StarSystem" or "System" or "SystemName" or "systemName", JsonTokenType.String):
                        data.SystemName = reader.GetString();
                        break;
                    case ("MarketID" or "marketId", JsonTokenType.Number) when reader.TryGetInt64(out long dv):
                        data.MarketId = dv;
                        break;
                    case ("StationName" or "stationName", JsonTokenType.String):
                        data.StationName = reader.GetString();
                        break;
                    case ("CarrierID", JsonTokenType.String):
                        data.StationName = reader.GetString();
                        data.StationType ??= "FleetCarrier";
                        break;
                    case ("Name", JsonTokenType.String) when data.Schema?.StartsWith("https://eddn.edcd.io/schemas/approachsettlement/1") == true:
                        data.StationName = reader.GetString();
                        break;
                    case ("StationType", JsonTokenType.String):
                        data.StationType = reader.GetString();
                        break;
                    case ("Latitude", JsonTokenType.Number) when reader.TryGetDecimal(out decimal dv):
                        data.Latitude = Math.Round(dv, 6);
                        break;
                    case ("Longitude", JsonTokenType.Number) when reader.TryGetDecimal(out decimal dv):
                        data.Longitude = Math.Round(dv, 6);
                        break;
                    case ("Name", JsonTokenType.String) when data.Schema?.StartsWith("https://eddn.edcd.io/schemas/codexentry/1") == true:
                        codexName = reader.GetString();
                        break;
                    case ("Category", JsonTokenType.String):
                        codexCategory = reader.GetString();
                        break;
                    case ("SubCategory", JsonTokenType.String):
                        codexSubCategory = reader.GetString();
                        break;
                    case ("Region", JsonTokenType.String):
                        codexRegion = reader.GetString();
                        break;
                    case ("EntryID", JsonTokenType.Number) when reader.TryGetInt64(out long dv):
                        codexEntryId = dv;
                        break;
                    case ("StarPos", JsonTokenType.StartArray):
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.Number);
                        Assert(reader.TryGetDecimal(out decimal xv));
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.Number);
                        Assert(reader.TryGetDecimal(out decimal yv));
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.Number);
                        Assert(reader.TryGetDecimal(out decimal zv));
                        Assert(reader.Read());
                        Assert(reader.TokenType == JsonTokenType.EndArray);
                        data.X = xv;
                        data.Y = yv;
                        data.Z = zv;
                        break;
                    case ("signals", JsonTokenType.StartArray) when data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fsssignaldiscovered/1") == true:
                        Assert(TryProcessSignals(ref reader, data));
                        break;
                    case ("Signals", JsonTokenType.StartArray) when data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fsssignaldiscovered/1") == false:
                        Assert(TryProcessBodySignals(ref reader, data));
                        break;
                    case ("Route", JsonTokenType.StartArray):
                        Assert(TryProcessNavRoute(ref reader, data));
                        break;
                    case ("Rings", JsonTokenType.StartArray):
                        Assert(TryProcessRings(ref reader, data));
                        break;
                    case ("odyssey", JsonTokenType.True or JsonTokenType.False):
                        data.IsOdyssey = reader.GetBoolean();
                        break;
                    case ("horizons", JsonTokenType.True or JsonTokenType.False):
                        data.IsHorizons = reader.GetBoolean();
                        break;
                    case ("event", JsonTokenType.String):
                        data.EventType = reader.GetString();
                        break;
                    case ("timestamp", JsonTokenType.String) when reader.TryGetDateTime(out var ts):
                        data.Timestamp = ts;
                        break;
                }
            }
        }

        if (data.SystemName != null)
        {
            if (data.BodyName != null)
            {
                data.BodyType ??= data.BodyName.Split(' ') switch
                {
                    [.. _, _, "A" or "B" or "C" or "D", "Belt"] => BodyType.StellarRing.ToString(),
                    [.. _, _, "A" or "B" or "C" or "D", "Ring"] => BodyType.PlanetaryRing.ToString(),
                    [.. _, _, "A" or "B" or "C" or "D", "Belt", "Cluster", string n] when int.TryParse(n, out _) => BodyType.AsteroidCluster.ToString(),
                    [.. _, _, "Comet", string n] when int.TryParse(n, out _) => BodyType.SmallBody.ToString(),
                    _ => null
                };

                if (data.BodyType == null && data.StationType == "SurfaceStation")
                {
                    data.BodyType = "Planet";
                }
            }
        }
        else
        {
            if (data.BodyName != null)
            {
                Fail("Body Name without System Name");
            }

            if (data.StationName != null
                && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fcmaterials_capi/1") != true
                && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fcmaterials/1") != true
                && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/dockingdenied/1") != true
                && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/dockinggranted/1") != true
                && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fcmaterials_journal/1") != true)
            {
                Fail($"Unknown schema {data.Schema}");
            }
        }

        if (codexName != null && data.BodySignalInfo.Count == 0)
        {
            data.BodySignalInfo[0] = (codexName, 0, codexCategory, codexSubCategory, codexRegion, codexEntryId);
        }

        return true;
    }

    private bool TryProcessLine(ReadOnlySequence<byte> line, FileLineData data)
    {
        var reader = new Utf8JsonReader(line);
        bool gotSchema = false;
        bool gotHeader = false;
        bool gotMessage = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 0) continue;

            if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1)
            {
                string? name = reader.GetString();

                while (reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.Comment)
                {
                    Assert(reader.Read());
                }

                switch ((name, reader.TokenType))
                {
                    case ("$schemaRef", JsonTokenType.String):
                        data.Schema = reader.GetString();
                        gotSchema = true;
                        break;
                    case ("header", JsonTokenType.StartObject):
                        Assert(TryProcessLineHeader(ref reader, data));
                        gotHeader = true;
                        break;
                    case ("message", JsonTokenType.StartObject):
                        Assert(TryProcessLineMessage(ref reader, line, data));
                        gotMessage = true;
                        break;
                    default:
                        break;
                }
            }
        }

        return gotSchema && gotMessage && gotHeader;
    }
}
