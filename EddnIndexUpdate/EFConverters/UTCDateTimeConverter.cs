using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace EddnIndexUpdate.EFConverters;

public class UTCDateTimeConverter(int precision)
    : ValueConverter<DateTime?, DateTime?>(
        TruncateDateTimeExpr(precision),
        AsUTCFunc()
    )
{
    private static Expression<Func<DateTime?, DateTime?>> TruncateDateTimeExpr(int precision)
    {
        var mult = precision switch
        {
            >= 7 => 1,
            6 => 10,
            5 => 100,
            4 => 1000,
            3 => 10000,
            2 => 100000,
            1 => 1000000,
            <= 0 => 10000000
        };

        return e => TruncateDateTime(e, mult);
    }

    private static DateTime? TruncateDateTime(DateTime? dt, int mult)
    {
        if (dt is not DateTime value)
        {
            return null;
        }

        var tick = (value.Ticks / mult) * mult;
        return new DateTime(tick);
    }

    private static Expression<Func<DateTime?, DateTime?>> AsUTCFunc()
        => e => AsUTC(e);

    private static DateTime? AsUTC(DateTime? dt)
    {
        if (dt is not DateTime value)
        {
            return null;
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
