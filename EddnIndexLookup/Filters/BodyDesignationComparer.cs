using EddnIndex.Common;
using EddnIndex.Common.Models;
using EddnIndexLookup.DTO;

namespace EddnIndexLookup.Filters
{
    /// <summary>
    /// Comparer for body designations
    /// </summary>
    public class BodyDesignationComparer(string sysname) : IComparer<BodyDesignation?>, IComparer<IBodyData>
    {
        private readonly string _systemName = sysname;

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
                || x.DesignationType == DesignationType.Unknown
                || y.DesignationType == DesignationType.Unknown)
            {
                return x.Designation.CompareTo(y.Designation);
            }

            var xhasplanet = x.DesignationType >= DesignationType.PlanetaryBody;
            var xhasmoon1 = x.DesignationType >= DesignationType.Moon1Body;
            var xhasmoon2 = x.DesignationType >= DesignationType.Moon2Body;
            var xhasmoon3 = x.DesignationType >= DesignationType.Moon3Body;

            var yhasplanet = y.DesignationType >= DesignationType.PlanetaryBody;
            var yhasmoon1 = y.DesignationType >= DesignationType.Moon1Body;
            var yhasmoon2 = y.DesignationType >= DesignationType.Moon2Body;
            var yhasmoon3 = y.DesignationType >= DesignationType.Moon3Body;

            var planetcomp = Nullable.Compare(x.PlanetNum, y.PlanetNum);
            var moon1comp = Nullable.Compare(x.Moon1Num, y.Moon1Num);
            var moon2comp = Nullable.Compare(x.Moon2Num, y.Moon2Num);
            var moon3comp = Nullable.Compare(x.Moon3Num, y.Moon3Num);

            if (x.DesignationType == DesignationType.StellarBarycentre
                && y.DesignationType != DesignationType.StellarBarycentre)
            {
                return -1;
            }

            if (y.DesignationType == DesignationType.StellarBarycentre
                && x.DesignationType != DesignationType.StellarBarycentre)
            {
                return 1;
            }

            if (x.DesignationType == DesignationType.StellarBarycentre
                && y.DesignationType == DesignationType.StellarBarycentre)
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

            if (x.DesignationType is DesignationType.Belt or DesignationType.AsteroidCluster
                && y.DesignationType is not (DesignationType.Belt or DesignationType.AsteroidCluster))
            {
                return -1;
            }

            if (y.DesignationType is DesignationType.Belt or DesignationType.AsteroidCluster
                && x.DesignationType is not (DesignationType.Belt or DesignationType.AsteroidCluster))
            {
                return 1;
            }

            if (x.DesignationType is DesignationType.Belt or DesignationType.AsteroidCluster
                && y.DesignationType is DesignationType.Belt or DesignationType.AsteroidCluster)
            {
                return planetcomp == 0 ? moon1comp : planetcomp;
            }

            foreach (var (bctype, (xnum, xhas), (ynum, yhas), cmp) in new[]
            {
                (DesignationType.PlanetaryBarycentre, (x.PlanetNum, xhasplanet), (y.PlanetNum, yhasplanet), planetcomp),
                (DesignationType.Moon1Barycentre, (x.Moon1Num, xhasmoon1), (y.Moon1Num, yhasmoon1), moon1comp),
                (DesignationType.Moon2Barycentre, (x.Moon2Num, xhasmoon2), (y.Moon2Num, yhasmoon2), moon2comp),
                (DesignationType.Moon3Barycentre, (x.Moon3Num, xhasmoon3), (y.Moon3Num, yhasmoon3), moon3comp),
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
            if (x?.Designation is null)
            {
                return y?.Designation is null ? 0 : 1;
            }

            if (y?.Designation is null)
            {
                return -1;
            }

            if (x.Designation == y.Designation)
            {
                return 0;
            }

            if (!x.Designation.StartsWith(_systemName))
            {
                return y.Designation.StartsWith(_systemName) ? 1 : x.Designation.CompareTo(y.Designation);
            }

            if (!y.Designation.StartsWith(_systemName))
            {
                return -1;
            }

            if (x.BodyDesignation is null || y.BodyDesignation is null)
            {
                return x.Designation.CompareTo(y.Designation);
            }

            return Compare(x.BodyDesignation, y.BodyDesignation);
        }
    }
}
