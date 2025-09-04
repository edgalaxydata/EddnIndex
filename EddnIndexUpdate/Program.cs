using EddnIndexUpdate;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

foreach (var arg in args)
{
    List<string> filenames = [
        .. Directory.EnumerateFiles(arg, "*.jsonl.bz2", SearchOption.AllDirectories),
        .. Directory.EnumerateFiles(arg, "*.jsonl", SearchOption.AllDirectories)
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
