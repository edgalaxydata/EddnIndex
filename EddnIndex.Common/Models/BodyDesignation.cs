namespace EddnIndex.Common.Models;

public record class BodyDesignation : IHasId<int>
{
    public int Id { get; set; }
    public int? DesignationId { get; set; }
    public required string Designation { get; init; }
    public required DesignationType DesignationType { get; init; }
    public int? StarNum { get; init; }
    public int? StellarBarycentreLength { get; init; }
    public int? PlanetNum { get; init; }
    public int? Moon1Num { get; init; }
    public int? Moon2Num { get; init; }
    public int? Moon3Num { get; init; }
    public int? BarycentreLength { get; init; }
    public int? RingNum { get; init; }
    public int? ClusterNum { get; init; }
    public int? CometNum { get; init; }

    public int? GetDesignationId()
    {
        if (StarNum < 0 || StarNum >= 16) return null;
        if (StellarBarycentreLength < 1 || StellarBarycentreLength >= 16) return null;
        if (StellarBarycentreLength != null && StarNum < 1) return null;
        if ((int)DesignationType < 1 || (int)DesignationType >= 32) return null;

        int id = int.MinValue | ((int)DesignationType << 26) | ((StarNum ?? 0) << 22) | ((StellarBarycentreLength ?? 0) << 18);

        return (DesignationType, PlanetNum, Moon1Num, Moon2Num, Moon3Num, BarycentreLength, RingNum, ClusterNum, CometNum) switch
        {
            (DesignationType.StellarBody or DesignationType.StellarBarycentre, null, null, null, null, null, null, null, null)
                => id,
            (DesignationType.Belt, null, null, null, null, null, int rNum, null, null)
                when (rNum >= 1 && rNum < 64)
                => id | (rNum << 12),
            (DesignationType.AsteroidCluster, null, null, null, null, null, int rNum, int cNum, null)
                when (rNum >= 1 && rNum < 64 && cNum >= 1 && cNum < 32)
                => id | (rNum << 12) | (cNum << 7),
            (DesignationType.Comet, null, null, null, null, null, null, null, int cNum)
                when (cNum >= 1 && cNum < 64)
                => id | (cNum << 12),
            (DesignationType.PlanetaryBody, int pNum, null, null, null, null, null, null, null)
                when (pNum >= 1 && pNum < 64)
                => id | (pNum << 12),
            (DesignationType.PlanetaryBarycentre, int pNum, null, null, null, int bLen, null, null, null)
                when (pNum >= 1 && pNum < 64 && bLen >= 2 && bLen < 32)
                => id | (pNum << 12) | (bLen << 7),
            (DesignationType.PlanetaryRing, int pNum, null, null, null, null, int rNum, null, null)
                when (pNum >= 1 && pNum < 64 && rNum >= 1 && rNum < 32)
                => id | (pNum << 12) | (rNum << 7),
            (DesignationType.PlanetaryComet, int pNum, null, null, null, null, null, null, int cNum)
                when (pNum >= 1 && pNum < 64 && cNum >= 1 && cNum < 32)
                => id | (pNum << 12) | (cNum << 7),
            (DesignationType.Moon1Body, int pNum, int m1, null, null, null, null, null, null)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32)
                => id | (pNum << 12) | (m1 << 7),
            (DesignationType.Moon1Barycentre, int pNum, int m1, null, null, int bLen, null, null, null)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && bLen >= 2 && bLen < 16)
                => id | (pNum << 12) | (m1 << 7) | (bLen << 3),
            (DesignationType.Moon1Ring, int pNum, int m1, null, null, null, int rNum, null, null)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && rNum >= 1 && rNum < 16)
                => id | (pNum << 12) | (m1 << 7) | (rNum << 3),
            (DesignationType.Moon1Comet, int pNum, int m1, null, null, null, null, null, int cNum)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && cNum >= 1 && cNum < 16)
                => id | (pNum << 12) | (m1 << 7) | (cNum << 3),
            (DesignationType.Moon2Body, int pNum, int m1, int m2, null, null, null, null, null)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && m2 >= 1 && m2 < 16)
                => id | (pNum << 12) | (m1 << 7) | (m2 << 3),
            (DesignationType.Moon2Barycentre, int pNum, int m1, int m2, null, int bLen, null, null, null)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && m2 >= 1 && m2 < 16 && bLen >= 2 && bLen < 8)
                => id | (pNum << 12) | (m1 << 7) | (m2 << 3) | bLen,
            (DesignationType.Moon2Ring, int pNum, int m1, int m2, null, null, int rNum, null, null)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && m2 >= 1 && m2 < 16 && rNum >= 1 && rNum < 8)
                => id | (pNum << 12) | (m1 << 7) | (m2 << 3) | rNum,
            (DesignationType.Moon2Comet, int pNum, int m1, int m2, null, null, null, null, int cNum)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && m2 >= 1 && m2 < 16 && cNum >= 1 && cNum < 8)
                => id | (pNum << 12) | (m1 << 7) | (m2 << 3) | cNum,
            (DesignationType.Moon3Body, int pNum, int m1, int m2, int m3, null, null, null, null)
                when (pNum >= 1 && pNum < 64 && m1 >= 1 && m1 < 32 && m2 >= 1 && m2 < 16 && m3 >= 1 && m3 < 8)
                => id | (pNum << 12) | (m1 << 7) | (m2 << 3) | m3,
            _ => null
        };
    }
}
