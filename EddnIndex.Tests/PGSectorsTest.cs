using EddnIndexUpdate.Sectors;

namespace EddnIndex.Tests;

public class PGSectorsTest
{
    // Top 100 most visited procgen sectors
    private static readonly Dictionary<string, int> SectorNameToId = new()
    {
        ["Synuefe"] = 151463,
        ["Wregoe"] = 151591,
        ["Eol Prou"] = 282527,
        ["Wredguia"] = 151590,
        ["Praea Euq"] = 159783,
        ["Synuefai"] = 151462,
        ["Outotz"] = 143399,
        ["Swoilz"] = 159655,
        ["Graea Hypue"] = 241574,
        ["Stuemeae"] = 323623,
        ["Swoiwns"] = 159654,
        ["Bleae Thua"] = 167974,
        ["Bleia Eohn"] = 167846,
        ["Blu Thua"] = 167975,
        ["Skaude"] = 216995,
        ["Eoch Flyuae"] = 266144,
        ["Nyeajaae"] = 200612,
        ["Prua Phoe"] = 225186,
        ["Clooku"] = 233378,
        ["Pru Euq"] = 159782,
        ["Blua Eaec"] = 249761,
        ["Smojue"] = 176166,
        ["Dryooe Flyou"] = 274335,
        ["Sifi"] = 159781,
        ["Bleia Dryiae"] = 184230,
        ["Flyiedge"] = 208803,
        ["Blae Drye"] = 184229,
        ["Traikaae"] = 184358,
        ["Plaa Eurk"] = 151589,
        ["Aucoks"] = 167847,
        ["Pro Eurl"] = 159784,
        ["Dryio Flyuae"] = 274336,
        ["Juenae"] = 323495,
        ["Droju"] = 176038,
        ["Drojeae"] = 176037,
        ["Eoch Pruae"] = 282528,
        ["Stuelou"] = 241569,
        ["Prua Dryoae"] = 159656,
        ["Prooe Drye"] = 159653,
        ["Outorst"] = 143397,
        ["Pyraleau"] = 192420,
        ["Blu Euq"] = 184359,
        ["Plio Eurl"] = 151592,
        ["Myrielk"] = 315431,
        ["Nuekuae"] = 241570,
        ["Gria Drye"] = 192421,
        ["Byoomao"] = 315302,
        ["Flyiedgiae"] = 208804,
        ["Boeph"] = 257952,
        ["Smojai"] = 176167,
        ["Byeia Eurk"] = 192549,
        ["Boelts"] = 257953,
        ["Oochost"] = 143270,
        ["Hegua"] = 135204,
        ["Skaudai"] = 216994,
        ["Pyramoe"] = 192422,
        ["Myriesly"] = 315430,
        ["Phylucs"] = 167976,
        ["Kyloarph"] = 290721,
        ["Pru Eurk"] = 159780,
        ["Traikoa"] = 184357,
        ["Aucopp"] = 167845,
        ["Outopps"] = 143398,
        ["Synuefue"] = 151461,
        ["Ceeckia"] = 569254,
        ["Phylur"] = 167973,
        ["Boewnst"] = 257954,
        ["Blaa Eork"] = 167848,
        ["Prieluia"] = 208935,
        ["Flyua Dryoae"] = 151464,
        ["Pru Aescs"] = 208932,
        ["Byeia Euq"] = 192551,
        ["Thaileia"] = 184231,
        ["Oochorrs"] = 143271,
        ["Byeia Thaa"] = 176165,
        ["Drojaea"] = 176039,
        ["Plaa Aescs"] = 200741,
        ["Dryooe Prou"] = 290719,
        ["Lysoosms"] = 200740,
        ["Kyloall"] = 290720,
        ["Sifeae"] = 159785,
        ["Traikee"] = 184360,
        ["Preia Phoe"] = 225187,
        ["Phroi Flyuae"] = 307108,
        ["Lysoorb"] = 200743,
        ["Phua Aub"] = 323622,
        ["Byua Euq"] = 192550,
        ["Oochoss"] = 143269,
        ["Blau Eur"] = 184362,
        ["Hegeia"] = 135205,
        ["Outordy"] = 143396,
        ["Spoihaae"] = 282399,
        ["Eafots"] = 118818,
        ["Hypio Pri"] = 315303,
        ["Bleae Thaa"] = 167972,
        ["Ploea Eurl"] = 151593,
        ["Schee Flyi"] = 298915,
        ["Scheau Flyi"] = 298914,
        ["Blaa Phoe"] = 249762,
        ["Zuni"] = 307239
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
