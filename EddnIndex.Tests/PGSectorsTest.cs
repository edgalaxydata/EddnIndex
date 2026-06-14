using EddnIndexUpdate.Sectors;

namespace EddnIndex.Tests;

public class PGSectorsTest
{
    private static readonly Dictionary<string, int> SectorNameToId = new()
    {
        ["Wregoe"] = 151591,
        ["Wredguia"] = 151590,
        ["Synuefe"] = 151463,
        ["Synuefai"] = 151462,
        ["Stuemeae"] = 323623,
        ["Eol Prou"] = 282527
    };

    [Test]
    public void TestC1SectorIdRoundTrip()
    {
        for (int sectorId = 0; sectorId < 128 * 64 * 128; sectorId++)
        {
            var pos = PGSectors.ByteXYZ.FromSectorId(sectorId);
            var name = PGSectors.GetC1SectorName(pos);
            Assert.That(PGSectors.GetSectorPos(name), Is.EqualTo(pos), $"C1 sector name did not round-trip for SectorId {sectorId} => Name {name}");
        }
    }

    [Test]
    public void TestC2SectorIdRoundTrip()
    {
        for (int sectorId = 0; sectorId < 128 * 64 * 128; sectorId++)
        {
            var pos = PGSectors.ByteXYZ.FromSectorId(sectorId);
            var name = PGSectors.GetC2SectorName(pos, true);
            Assert.That(PGSectors.GetSectorPos(name), Is.EqualTo(pos), $"C2 sector name did not round-trip for SectorId {sectorId} => Name {name}");
        }
    }

    [Test]
    public void TestSectorNameToId()
    {
        foreach (var (name, id) in SectorNameToId)
        {
            Assert.That(PGSectors.GetSectorPos(name).SectorId, Is.EqualTo(id), $"Sector name to id mapping failed for Name {name}");
        }
    }

    [Test]
    public void TestSectorIdToName()
    {
        foreach (var (name, id) in SectorNameToId)
        {
            Assert.That(PGSectors.GetSectorName(id), Is.EqualTo(name), $"Sector id to name mapping failed for SectorId {id}");
        }
    }
}
