using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace EddnIndexUpdate;

public static class DBConfigurationOptionsExtensions
{
    public static void ConfigureDB(this DbContextOptionsBuilder opts, IConfigurationSection section)
    {
        var dbsettings = section.Get<Dictionary<string, object>>() ?? [];
        dbsettings.Remove("Provider");
        var provider = section.GetValue<string>("Provider")?.ToLowerInvariant();
        DbConnectionStringBuilder csb;

        if (provider == "mysql" || provider == "mariadb")
        {
            csb = new MySqlConnector.MySqlConnectionStringBuilder();
            dbsettings.Remove("ServerVersion");
        }
        else if (provider == "npgsql")
        {
            csb = new Npgsql.NpgsqlConnectionStringBuilder();
        }
        else if (provider == "sqlite")
        {
            csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder();
        }
        else if (provider == "mssql" || provider == "sqlserver" || provider == null)
        {
            csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder();
        }
        else
        {
            throw new NotSupportedException();
        }

        foreach (var (name, value) in dbsettings)
        {
            var prop = csb.GetType().GetProperty(name);

            if (prop != null)
            {
                if (prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(csb, Convert.ToBoolean(value));
                }
                else if (prop.PropertyType == typeof(string))
                {
                    prop.SetValue(csb, Convert.ToString(value));
                }
                else if (typeof(IConvertible).IsAssignableFrom(prop.PropertyType))
                {
                    prop.SetValue(csb, Convert.ChangeType(value, prop.PropertyType));
                }
            }
            else
            {
                csb[name] = value;
            }
        }

        var connstring = csb.ToString();

        if (provider == "mariadb")
        {
            opts.UseMySql(
                connstring,
                new MariaDbServerVersion(section.GetValue<string>("ServerVersion") ?? "11.0"),
                dbopts => dbopts.MigrationsAssembly("EddnIndexUpdate.Migrations_MariaDB")
            );
            opts.AddInterceptors(new UTCTimeInterceptor());
        }
        else if (provider == "mysql")
        {
            opts.UseMySql(
                connstring,
                new MySqlServerVersion(section.GetValue<string>("ServerVersion") ?? "8.0"),
                dbopts => dbopts.MigrationsAssembly("EddnIndexUpdate.Migrations_MySQL")
            );
            opts.AddInterceptors(new UTCTimeInterceptor());
        }
        else if (provider == "npgsql")
        {
            opts.UseNpgsql(
                connstring,
                dbopts => dbopts.MigrationsAssembly("EddnIndexUpdate.Migrations_Npgsql")
            );
        }
        else if (provider == "sqlite")
        {
            opts.UseSqlite(
                connstring,
                dbopts => dbopts.MigrationsAssembly("EddnIndexUpdate.Migrations_Sqlite")
            );
        }
        else
        {
            opts.UseSqlServer(
                connstring,
                dbopts => dbopts.MigrationsAssembly("EddnIndexUpdate.Migrations_SqlServer")
            );
        }
    }
}
