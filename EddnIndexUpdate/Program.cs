using EddnIndexUpdate;
using EddnIndexUpdate.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Abstractions;

var cmdlineargs = new Dictionary<string, string?>();
var dirnames = new List<string>();

var mapopts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["exit-on-bad-data"] = "FileProcessor:ExitOnBadData",
    ["break-on-bad-data"] = "FileProcessor:BreakOnBadData",
    ["basedir"] = "FileProcessor:BaseDir",
    ["reprocess"] = "FileProcessor:Reprocess",
    ["wait"] = "WaitForDebugger",
};

for (int i = 0; i < args.Length; i++)
{
    if (args[i].StartsWith("--"))
    {
        string key, value;

        switch (args[i], args[i].Split('=', 2), args.Length > i + 1 && !args[i + 1].StartsWith("--") ? args[i + 1] : null)
        {
            case (_, [string opt, string optarg], _):
                key = opt[2..];
                value = optarg;
                break;
            case (string opt, _, string optarg):
                key = opt[2..];
                value = optarg;
                i++;
                break;
            case (string opt, _, _) when (opt.StartsWith("--no-", StringComparison.OrdinalIgnoreCase)):
                key = opt[5..];
                value = "false";
                break;
            default:
                key = args[i];
                value = "true";
                break;
        }

        if (mapopts.TryGetValue(key, out var mapkey))
        {
            key = mapkey;
        }

        cmdlineargs[key] = value;
    }
    else
    {
        dirnames.Add(args[i]);
    }
}

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile("hosting.json", optional: true);
builder.Configuration.AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true);
builder.Configuration.AddInMemoryCollection(cmdlineargs);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IFileSystem, Testably.Abstractions.RealFileSystem>();

builder.Services.AddDbContextFactory<EddnIndexUpdate.Models.EDDNContext>(opts => opts.ConfigureDB(builder.Configuration.GetSection("Database")));

builder.Services.AddOptions<FileProcessorSettings>()
                .BindConfiguration("FileProcessor")
                .ValidateDataAnnotations()
                .ValidateOnStart();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

using var host = builder.Build();
var svcprov = host.Services;

var processor = svcprov.GetRequiredService<FileProcessor>();

if (builder.Configuration.GetValue<bool?>("WaitForDebugger") == true)
{
    while (!Debugger.IsAttached)
    {
        Thread.Sleep(500);
    }

    Debugger.Break();
}

await processor.ProcessDirectoriesAsync(dirnames);
