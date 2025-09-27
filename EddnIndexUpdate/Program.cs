using EddnIndexUpdate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

var config = new ConfigurationBuilder()
    .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
    .AddJsonFile("appsettings.json", optional: true)
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

var dirnames = new List<string>();
bool wait = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i].StartsWith("--"))
    {
        switch (args[i], args[i].Split('=', 2), args.Length > i + 1)
        {
            case ("--wait", _, _):
                wait = true;
                break;
            default:
                Console.Error.WriteLine($"Unrecognized option {args[i]}");
                return;
        }
    }
    else
    {
        dirnames.Add(args[i]);
    }
}

if (wait)
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
