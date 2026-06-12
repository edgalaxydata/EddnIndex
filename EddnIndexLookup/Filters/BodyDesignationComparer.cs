using EddnIndexLookup.DTO;
using EddnIndexUpdate.Models;

namespace EddnIndexLookup.Filters
{
    /// <summary>
    /// Comparer for body designations
    /// </summary>
    public class BodyDesignationComparer(string sysname) : IComparer<BodyDesignation?>, IComparer<IBodyData>
    {
        private readonly string SystemName = sysname;

        /// <inheritdoc/>
        public int Compare(BodyDesignation? x, BodyDesignation? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return 1;
            }

            if (y is null)
            {
                return -1;
            }

            if (x.Designation == y.Designation)
            {
                return 0;
            }

            if ((x.StarNum == 0 && y.StarNum != 0)
                || (x.StarNum != 0 && y.StarNum == 0)
                || x.DesignationType == EddnIndexUpdate.DesignationType.Unknown
                || y.DesignationType == EddnIndexUpdate.DesignationType.Unknown)
            {
                return x.Designation.CompareTo(y.Designation);
            }

            var xhasplanet = x.DesignationType >= EddnIndexUpdate.DesignationType.PlanetaryBody;
            var xhasmoon1 = x.DesignationType >= EddnIndexUpdate.DesignationType.Moon1Body;
            var xhasmoon2 = x.DesignationType >= EddnIndexUpdate.DesignationType.Moon2Body;
            var xhasmoon3 = x.DesignationType >= EddnIndexUpdate.DesignationType.Moon3Body;

            var yhasplanet = y.DesignationType >= EddnIndexUpdate.DesignationType.PlanetaryBody;
            var yhasmoon1 = y.DesignationType >= EddnIndexUpdate.DesignationType.Moon1Body;
            var yhasmoon2 = y.DesignationType >= EddnIndexUpdate.DesignationType.Moon2Body;
            var yhasmoon3 = y.DesignationType >= EddnIndexUpdate.DesignationType.Moon3Body;

            var planetcomp = Nullable.Compare(x.PlanetNum, y.PlanetNum);
            var moon1comp = Nullable.Compare(x.Moon1Num, y.Moon1Num);
            var moon2comp = Nullable.Compare(x.Moon2Num, y.Moon2Num);
            var moon3comp = Nullable.Compare(x.Moon3Num, y.Moon3Num);

            if (x.DesignationType == EddnIndexUpdate.DesignationType.StellarBarycentre
                && y.DesignationType != EddnIndexUpdate.DesignationType.StellarBarycentre)
            {
                return -1;
            }

            if (y.DesignationType == EddnIndexUpdate.DesignationType.StellarBarycentre
                && x.DesignationType != EddnIndexUpdate.DesignationType.StellarBarycentre)
            {
                return 1;
            }

            if (x.DesignationType == EddnIndexUpdate.DesignationType.StellarBarycentre
                && y.DesignationType == EddnIndexUpdate.DesignationType.StellarBarycentre)
            {
                if (x.StarNum == y.StarNum && x.StellarBarycentreLength == y.StellarBarycentreLength)
                {
                    return 0;
                }
                else if (x.StarNum <= y.StarNum && x.StarNum + x.StellarBarycentreLength >= y.StarNum + y.StellarBarycentreLength)
                {
                    return -1;
                }
                else if (x.StarNum >= y.StarNum && x.StarNum + x.StellarBarycentreLength <= y.StarNum + y.StellarBarycentreLength)
                {
                    return 1;
                }
                else
                {
                    return Nullable.Compare(x.StarNum, y.StarNum);
                }
            }

            if (x.DesignationType is EddnIndexUpdate.DesignationType.Belt or EddnIndexUpdate.DesignationType.AsteroidCluster
                && y.DesignationType is not (EddnIndexUpdate.DesignationType.Belt or EddnIndexUpdate.DesignationType.AsteroidCluster))
            {
                return -1;
            }

            if (y.DesignationType is EddnIndexUpdate.DesignationType.Belt or EddnIndexUpdate.DesignationType.AsteroidCluster
                && x.DesignationType is not (EddnIndexUpdate.DesignationType.Belt or EddnIndexUpdate.DesignationType.AsteroidCluster))
            {
                return 1;
            }

            if (x.DesignationType is EddnIndexUpdate.DesignationType.Belt or EddnIndexUpdate.DesignationType.AsteroidCluster
                && y.DesignationType is EddnIndexUpdate.DesignationType.Belt or EddnIndexUpdate.DesignationType.AsteroidCluster)
            {
                return planetcomp == 0 ? moon1comp : planetcomp;
            }

            foreach (var (bctype, (xnum, xhas), (ynum, yhas), cmp) in new[]
            {
                (EddnIndexUpdate.DesignationType.PlanetaryBarycentre, (x.PlanetNum, xhasplanet), (y.PlanetNum, yhasplanet), planetcomp),
                (EddnIndexUpdate.DesignationType.Moon1Barycentre, (x.Moon1Num, xhasmoon1), (y.Moon1Num, yhasmoon1), moon1comp),
                (EddnIndexUpdate.DesignationType.Moon2Barycentre, (x.Moon2Num, xhasmoon2), (y.Moon2Num, yhasmoon2), moon2comp),
                (EddnIndexUpdate.DesignationType.Moon3Barycentre, (x.Moon3Num, xhasmoon3), (y.Moon3Num, yhasmoon3), moon3comp),
            })
            {
                if (x.DesignationType == bctype || y.DesignationType == bctype)
                {
                    if (xnum == ynum && x.DesignationType == y.DesignationType && x.BarycentreLength == y.BarycentreLength)
                    {
                        return 0;
                    }

                    if (x.DesignationType == bctype
                        && xnum <= ynum
                        && xnum + x.BarycentreLength >= ynum + (y.DesignationType == bctype ? y.BarycentreLength : 1))
                    {
                        return -1;
                    }

                    if (y.DesignationType == bctype
                        && ynum <= xnum && ynum + y.BarycentreLength >= xnum + (x.DesignationType == bctype ? x.BarycentreLength : 1))
                    {
                        return 1;
                    }

                    return cmp;
                }

                switch (xhas, yhas, cmp)
                {
                    case (false, false, not 0):
                        return cmp;
                    case (false, false, _):
                        return x.Designation.CompareTo(y.Designation);
                    case (true, false, _):
                        return 1;
                    case (false, true, _):
                        return -1;
                    case (true, true, not 0):
                        return cmp;
                }
            }

            return x.Designation.CompareTo(y.Designation);
        }

        /// <inheritdoc/>
        public int Compare(IBodyData? x, IBodyData? y)
        {
            return (x?.Designation?.StartsWith(SystemName), y?.Designation?.StartsWith(SystemName), x?.BodyDesignation, y?.BodyDesignation) switch
            {
                (null, null, _, _) => 0,
                (null, not null, _, _) => 1,
                (not null, null, _, _) => -1,
                (not null, not null, _, _) when (x.Designation == y.Designation) => 0,
                (false, false, _, _) => x.Designation.CompareTo(y.Designation),
                (false, _, _, _) => 1,
                (_, false, _, _) => -1,
                (_, _, null, _) or (_, _, _, null) => x.Designation.CompareTo(y.Designation),
                _ => Compare(x.BodyDesignation, y.BodyDesignation)
            };
        }
    }
}
