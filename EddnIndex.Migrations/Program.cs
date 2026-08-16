using EddnIndex.Common;
using EddnIndex.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

string? providerName = null;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--provider" && i < args.Length - 1)
    {
        providerName = args[i + 1];
        i++;
    }
}

var asms = AppDomain.CurrentDomain.GetAssemblies();
var curasm = Assembly.GetExecutingAssembly();
string curasmname = Path.GetFileNameWithoutExtension(curasm.Location);
string? curasmloc = Path.GetDirectoryName(curasm.Location);
string prefix = curasmname + ".";

foreach (var asm in asms)
{
    string? asmloc = Path.GetDirectoryName(asm.Location);
    string asmname = Path.GetFileNameWithoutExtension(asm.Location);

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

string sectionName = "Database_" + providerName;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile("hosting.json", optional: true);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

if (builder.Configuration.GetSection(sectionName).GetValue<string>("Provider") != providerName)
{
    throw new InvalidOperationException($"Provider {providerName} not configured");
}

builder.Services.AddDbContextFactory<EDDNContext>(opts => opts.ConfigureDB(builder.Configuration.GetSection(sectionName)));

using var host = builder.Build();
