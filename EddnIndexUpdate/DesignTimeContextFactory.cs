using EddnIndexUpdate.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;

namespace EddnIndexUpdate
{
    public class DesignTimeContextFactory : IDesignTimeDbContextFactory<EDDNContext>
    {
        public EDDNContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            string? providerName = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--provider" && i > args.Length - 1)
                {
                    providerName = args[i + 1];
                    i++;
                }
            }

            var asms = AppDomain.CurrentDomain.GetAssemblies();
            var curasm = Assembly.GetExecutingAssembly();
            var curasmname = Path.GetFileNameWithoutExtension(curasm.Location);
            var curasmloc = Path.GetDirectoryName(curasm.Location);
            var prefix = curasmname + ".Migrations_";

            foreach (var asm in asms)
            {
                var asmloc = Path.GetDirectoryName(asm.Location);
                var asmname = Path.GetFileNameWithoutExtension(asm.Location);

                if (asmloc == curasmloc
                    && asmname.StartsWith(prefix))
                {
                    providerName = asmname[prefix.Length..];
                }
            }

            if (providerName == null)
            {
                throw new InvalidOperationException("Specify a project and provider");
            }

            var sectionName = "Database_" + providerName;

            if (config.GetSection(sectionName).GetValue<string>("Provider") != providerName)
            {
                throw new InvalidOperationException($"Provider {providerName} not configured");
            }

            var ctxopts = new DbContextOptionsBuilder<EDDNContext>();
            ctxopts.ConfigureDB(config.GetSection(sectionName));
            return new EDDNContext(ctxopts.Options);
        }
    }
}
