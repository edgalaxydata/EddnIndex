using EddnIndexUpdate.Sectors;
using System;
using System.Collections.Generic;
using System.Text;

namespace EddnIndex.Tests;

public class PGSectorsTest
{
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
            var name = PGSectors.GetC2SectorName(pos);
            Assert.That(PGSectors.GetSectorPos(name), Is.EqualTo(pos), $"C2 sector name did not round-trip for SectorId {sectorId} => Name {name}");
        }
    }
}
