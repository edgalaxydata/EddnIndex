using Microsoft.EntityFrameworkCore;

namespace EddnIndex.Common;

public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueConstraintViolation(this DbUpdateException exception)
    {
        return exception.InnerException switch
        {
            Microsoft.Data.SqlClient.SqlException sqlEx
                when sqlEx.Number is 2601 or 2627
                => true,

            Npgsql.PostgresException pgEx
                when pgEx.SqlState == "23505"
                => true,

            Microsoft.Data.Sqlite.SqliteException sqliteEx
                when sqliteEx.SqliteErrorCode == 19
                => true,

            MySqlConnector.MySqlException mySqlEx
                when mySqlEx.Number == 1062
                => true,

            _ => false
        };
    }
}
