using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace EddnIndexUpdate
{
    public class UTCTimeInterceptor : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            command.CommandText = $"SET SESSION time_zone = '+00:00'; {command.CommandText}";
            return result;
        }
    }
}
