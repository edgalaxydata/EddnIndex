using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace EddnIndex.Common.EFConverters;

public class UTCDateTimeConverterNonNull(int precision)
    : ValueConverter<DateTime, DateTime>(
        TruncateDateTimeExpr(precision),
        AsUTCFunc()
    )
{
    private static Expression<Func<DateTime, DateTime>> TruncateDateTimeExpr(int precision)
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

    private static DateTime TruncateDateTime(DateTime value, int mult)
    {
        var tick = (value.Ticks / mult) * mult;
        return new DateTime(tick, DateTimeKind.Unspecified);
    }

    private static Expression<Func<DateTime, DateTime>> AsUTCFunc()
        => e => AsUTC(e);

    private static DateTime AsUTC(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
