using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Data.Common;

namespace EddnIndex.Common;

public static class DBConfigurationOptionsExtensions
{
    public static void ConfigureDB(this DbContextOptionsBuilder opts, IConfigurationSection section)
    {
        var dbsettings = section.Get<Dictionary<string, object>>() ?? [];
        dbsettings.Remove("Provider");
        dbsettings.Remove("ServerVersion");
        string? provider = section.GetValue<string>("Provider")?.ToLowerInvariant();
        DbConnectionStringBuilder csb = provider switch
        {
            "mysql" or "mariadb" => new MySqlConnector.MySqlConnectionStringBuilder(),
            "npgsql" => new Npgsql.NpgsqlConnectionStringBuilder(),
            "sqlite" => new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(),
            "mssql" or "sqlserver" or null => new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(),
            _ => throw new NotSupportedException(),
        };

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

        string connstring = csb.ToString();

        if (provider == "mariadb")
        {
            opts.UseMySql(
                connstring,
                new MariaDbServerVersion(section.GetValue<string>("ServerVersion") ?? "11.0"),
                dbopts => dbopts.MigrationsAssembly("EddnIndex.Migrations.MariaDB")
            );

            opts.AddInterceptors(new UTCTimeInterceptor());
        }
        else if (provider == "mysql")
        {
            opts.UseMySql(
                connstring,
                new MySqlServerVersion(section.GetValue<string>("ServerVersion") ?? "8.0"),
                dbopts => dbopts.MigrationsAssembly("EddnIndex.Migrations.MySQL")
            );

            opts.AddInterceptors(new UTCTimeInterceptor());
        }
        else if (provider == "npgsql")
        {
            opts.UseNpgsql(
                connstring,
                dbopts => dbopts.MigrationsAssembly("EddnIndex.Migrations.Npgsql")
            );
        }
        else if (provider == "sqlite")
        {
            opts.UseSqlite(
                connstring,
                dbopts => dbopts.MigrationsAssembly("EddnIndex.Migrations.Sqlite")
            );
        }
        else
        {
            opts.UseSqlServer(
                connstring,
                dbopts => dbopts.MigrationsAssembly("EddnIndex.Migrations.SqlServer")
            );
        }
    }
}
