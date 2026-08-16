using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EddnIndex.Common;

public class UTCTimeInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        command.CommandText = $"SET SESSION time_zone = '+00:00'; {command.CommandText}";
        return result;
    }
}
