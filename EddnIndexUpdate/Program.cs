using EddnIndexUpdate;
using EddnIndexUpdate.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("hosting.json", optional: true)
    .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
    .AddInMemoryCollection(cmdlineargs)
    .Build();

var services = new ServiceCollection();

services.AddDbContextFactory<EddnIndexUpdate.Models.EDDNContext>(opts => opts.ConfigureDB(config.GetSection("Database")));
services.Configure<FileProcessorSettings>(config.GetSection("FileProcessor"));
services.AddTransient<FileProcessor>();

services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.AddConfiguration(config.GetSection("Logging"));
});

var svcprov = services.BuildServiceProvider();

var processor = svcprov.GetRequiredService<FileProcessor>();

if (config.GetValue<bool?>("WaitForDebugger") == true)
{
    while (!Debugger.IsAttached)
    {
        Thread.Sleep(500);
    }

    Debugger.Break();
}

foreach (var dirname in dirnames)
{
    List<string> filenames = [
        .. Directory.EnumerateFiles(dirname, "*.jsonl.bz2", SearchOption.AllDirectories),
        .. Directory.EnumerateFiles(dirname, "*.jsonl", SearchOption.AllDirectories)
    ];

    filenames = [..
        filenames
            .Select(e => (Parts: Path.GetFileNameWithoutExtension(e).Split("-"), Name: e))
            .OrderBy(e => e.Parts[^3])
            .ThenBy(e => e.Parts[^2])
            .ThenBy(e => e.Parts[^1])
            .Select(e => e.Name)];


    foreach (var filename in filenames)
    {
        processor.ProcessFile(filename);
    }
}
