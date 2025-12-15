using System.Text;
using System.Text.Json;

namespace EddnIndexUpdate
{
    public partial class FileProcessor
    {
        private bool TryProcessLineHeader(ref Utf8JsonReader reader, ref FileLineData data)
        {
            string? softwareName = null;
            string? softwareVersion = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.StartObject && reader.CurrentDepth == 1) continue;
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 1) break;

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 2)
                {
                    var name = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((name, reader.TokenType))
                    {
                        case ("gatewayTimestamp", JsonTokenType.String) when (reader.TryGetDateTime(out var gwts)):
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
                        default:
                            Fail($"Unknown header field {name}");
                            break;
                    }
                }
            }

            //Assert(softwareName != null && softwareVersion != null);

            data.Software = GetOrAddSoftware(softwareName ?? "", softwareVersion ?? "");

            return true;
        }

        private bool TryProcessNavRoute(ref Utf8JsonReader reader, ref FileLineData data)
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

                    data.NavRouteSystems[itemnum] = GetOrAddSystem(systemName, systemAddress, x, y, z);
                }

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 4)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((propname, reader.TokenType))
                    {
                        case ("SystemAddress", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            systemAddress = dv;
                            break;
                        case ("StarSystem", JsonTokenType.String):
                            systemName = reader.GetString();
                            break;
                        case ("StarPos", JsonTokenType.StartArray):
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var xv));
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var yv));
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var zv));
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

        private bool TryProcessSignals(ref Utf8JsonReader reader, ref FileLineData data)
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

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
                {
                    if (name != null)
                    {
                        data.Signals[itemnum] = GetOrAddSignal(name, type, isStation);
                    }
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
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

        private bool TryProcessBodySignals(ref Utf8JsonReader reader, ref FileLineData data)
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

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
                {
                    if (type != null)
                    {
                        data.BodySignals[itemnum] = GetOrAddBodySignal(type, count);
                    }
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((propname, reader.TokenType))
                    {
                        case ("Count", JsonTokenType.Number) when (reader.TryGetInt32(out var cnt)):
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

        private bool TryProcessRings(ref Utf8JsonReader reader, ref FileLineData data)
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

                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 3)
                {
                    if (ringName != null)
                    {
                        data.RingData[itemnum] = (ringName, innerRadius, outerRadius);
                    }
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propname = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    switch ((propname, reader.TokenType))
                    {
                        case ("Name", JsonTokenType.String):
                            ringName = reader.GetString();
                            break;
                        case ("InnerRadius", JsonTokenType.Number) when reader.TryGetDecimal(out var dv):
                            innerRadius = dv;
                            break;
                        case ("OuterRadius", JsonTokenType.Number) when reader.TryGetDecimal(out var dv):
                            outerRadius = dv;
                            break;
                    }
                }
            }

            return true;
        }

        private bool TryProcessLineMessage(ref Utf8JsonReader reader, ReadOnlySpan<byte> json, ref FileLineData data)
        {
            string? bodyName = null;
            string? bodyType = null;
            int? bodyId = null;
            long? systemAddress = null;
            long? marketId = null;
            string? systemName = null;
            string? stationName = null;
            string? stationType = null;
            string? parentsJson = null;
            string? codexName = null;
            string? codexCategory = null;
            string? codexSubCategory = null;
            string? codexRegion = null;
            long? codexEntryId = null;
            decimal? x = null;
            decimal? y = null;
            decimal? z = null;
            decimal? argOfPeriapsis = null;
            decimal? inclination = null;
            decimal? semiMajorAxis = null;
            decimal? latitude = null;
            decimal? longitude = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 1) break;

                if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 2)
                {
                    var propname = reader.GetString();

                    Assert(propname != null);

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
                    {
                        Assert(reader.Read());
                    }

                    data.MessageKeyCounts[(propname, reader.TokenType)] = data.MessageKeyCounts.GetValueOrDefault((propname, reader.TokenType)) + 1;

                    switch ((propname, reader.TokenType))
                    {
                        case ("Body" or "BodyName", JsonTokenType.String):
                            bodyName = reader.GetString();
                            break;
                        case ("BodyID", JsonTokenType.Number) when (reader.TryGetInt32(out int bid)):
                            bodyId = bid;
                            break;
                        case ("BodyType", JsonTokenType.String):
                            bodyType = reader.GetString();
                            break;
                        case ("Parents", JsonTokenType.StartArray):
                            var pos = (int)reader.TokenStartIndex;
                            reader.Skip();
                            var span = json[pos..(int)(reader.TokenStartIndex + 1)];
                            parentsJson = Encoding.UTF8.GetString(span);
                            break;
                        case ("Periapsis", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            argOfPeriapsis = dv;
                            break;
                        case ("OrbitalInclination", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            inclination = dv;
                            break;
                        case ("SemiMajorAxis", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            semiMajorAxis = dv;
                            break;
                        case ("StarType", JsonTokenType.String):
                            bodyType ??= "Star";
                            break;
                        case ("PlanetClass", JsonTokenType.String):
                            bodyType ??= "Planet";
                            break;
                        case ("SystemAddress", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            systemAddress = dv;
                            break;
                        case ("StarSystem" or "System" or "SystemName" or "systemName", JsonTokenType.String):
                            systemName = reader.GetString();
                            break;
                        case ("MarketID" or "marketId", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            marketId = dv;
                            break;
                        case ("StationName" or "stationName", JsonTokenType.String):
                            stationName = reader.GetString();
                            break;
                        case ("CarrierID", JsonTokenType.String):
                            stationName = reader.GetString();
                            stationType ??= "FleetCarrier";
                            break;
                        case ("Name", JsonTokenType.String) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/approachsettlement/1") == true):
                            stationName = reader.GetString();
                            break;
                        case ("StationType", JsonTokenType.String):
                            stationType = reader.GetString();
                            break;
                        case ("Latitude", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            latitude = Math.Round(dv, 6);
                            break;
                        case ("Longitude", JsonTokenType.Number) when (reader.TryGetDecimal(out var dv)):
                            longitude = Math.Round(dv, 6);
                            break;
                        case ("Name", JsonTokenType.String) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/codexentry/1") == true):
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
                        case ("EntryID", JsonTokenType.Number) when (reader.TryGetInt64(out var dv)):
                            codexEntryId = dv;
                            break;
                        case ("StarPos", JsonTokenType.StartArray):
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var xv));
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var yv));
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.Number);
                            Assert(reader.TryGetDecimal(out var zv));
                            Assert(reader.Read());
                            Assert(reader.TokenType == JsonTokenType.EndArray);
                            x = xv;
                            y = yv;
                            z = zv;
                            break;
                        case ("signals", JsonTokenType.StartArray) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fsssignaldiscovered/1") == true):
                            Assert(TryProcessSignals(ref reader, ref data));
                            break;
                        case ("Signals", JsonTokenType.StartArray) when (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fsssignaldiscovered/1") == false):
                            Assert(TryProcessBodySignals(ref reader, ref data));
                            break;
                        case ("Route", JsonTokenType.StartArray):
                            Assert(TryProcessNavRoute(ref reader, ref data));
                            break;
                        case ("Rings", JsonTokenType.StartArray):
                            Assert(TryProcessRings(ref reader, ref data)); ;
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
                        case ("timestamp", JsonTokenType.String) when (reader.TryGetDateTime(out var ts)):
                            data.Timestamp = ts;
                            break;
                    }
                }
            }

            if (systemName != null)
            {
                var system = GetOrAddSystem(systemName, systemAddress, x, y, z);
                data.System = system;

                if (bodyName != null)
                {
                    bodyType ??= bodyName.Split(' ') switch
                    {
                        [.. _, _, "A" or "B" or "C" or "D", "Belt"] => BodyType.StellarRing.ToString(),
                        [.. _, _, "A" or "B" or "C" or "D", "Ring"] => BodyType.PlanetaryRing.ToString(),
                        [.. _, _, "A" or "B" or "C" or "D", "Belt", "Cluster", string n] when int.TryParse(n, out _) => BodyType.AsteroidCluster.ToString(),
                        [.. _, _, "Comet", string n] when int.TryParse(n, out _) => BodyType.SmallBody.ToString(),
                        _ => null
                    };

                    if (bodyType == null && stationType == "SurfaceStation")
                    {
                        bodyType = "Planet";
                    }

                    var (body, smaerror, incerror, aoperror) = GetOrAddBody(bodyName, systemName, bodyId, bodyType, parentsJson, argOfPeriapsis, inclination, semiMajorAxis, data.Timestamp, data.GameVersion, system);
                    data.Body = body;
                    data.SemiMajorAxisError = smaerror;
                    data.InclinationError = incerror;
                    data.ArgOfPeriapsisError = aoperror;
                }

                foreach (var (itemnum, (name, innerRad, outerRad)) in data.RingData)
                {
                    data.SubBodies[itemnum] = GetOrAddBody(name, systemName, null, null, null, 0, 0, (innerRad + outerRad) / 2, data.Timestamp, data.GameVersion, system);
                }
            }
            else if (bodyName != null)
            {
                Fail("Body Name without System Name");
            }
            else if (stationName != null)
            {
                if (data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fcmaterials_capi/1") != true
                    && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/dockingdenied/1") != true
                    && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/dockinggranted/1") != true
                    && data.Schema?.StartsWith("https://eddn.edcd.io/schemas/fcmaterials_journal/1") != true)
                {
                    Fail($"Unknown schema {data.Schema}");
                }
            }

            if (codexName != null && data.BodySignals.Count == 0)
            {
                data.BodySignals[0] = GetOrAddBodySignal(codexName, 0, codexCategory, codexSubCategory, codexRegion, codexEntryId);
                data.Latitude = latitude;
                data.Longitude = longitude;
            }

            if (stationName != null || marketId != null)
            {
                data.Station = GetOrAddStation(stationName, marketId, stationType, systemName, systemAddress, bodyName, latitude, longitude);
                data.Latitude = latitude;
                data.Longitude = longitude;
            }

            return true;
        }

        private bool TryProcessLine(ReadOnlySpan<byte> line, ref FileLineData data)
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
                    var name = reader.GetString();

                    while (reader.TokenType == JsonTokenType.PropertyName || reader.TokenType == JsonTokenType.Comment)
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
                            Assert(TryProcessLineHeader(ref reader, ref data));
                            gotHeader = true;
                            break;
                        case ("message", JsonTokenType.StartObject):
                            Assert(TryProcessLineMessage(ref reader, line, ref data));
                            gotMessage = true;
                            break;
                        default:
                            break;
                    }
                }
            }

            data.GameVersionInfo = GetOrAddGameVersion(data.GameBuild, data.GameVersion, data.IsOdyssey, data.IsHorizons);

            return gotSchema && gotMessage && gotHeader;
        }
    }
}
