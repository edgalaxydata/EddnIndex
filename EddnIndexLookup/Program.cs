using EddnIndexUpdate;
using EddnIndexLookup.Services;
using System.Reflection;
using EddnIndexUpdate.Options;
using EddnIndexLookup.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile("hosting.json", optional: true);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    opts.JsonSerializerOptions.PropertyNamingPolicy = null;
                    opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

builder.Services.AddDbContextFactory<EddnIndexUpdate.Models.EDDNContext>(
    opts =>
    {
        opts.ConfigureDB(builder.Configuration.GetSection("Database"));

        if (builder.Environment.IsDevelopment())
        {
            opts.EnableSensitiveDataLogging(true);
        }
    }
);
builder.Services.Configure<EddnLookupServiceSettings>(builder.Configuration.GetSection("APIService"));
builder.Services.AddTransient<EddnLookupService>();
builder.Services.AddSwaggerGen(options =>
{
    options.UseAllOfForInheritance();
    options.UseOneOfForPolymorphism();

    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Version = "v1",
        Title = "EDDN Index Lookup",
        Description = "API for querying EDDN capture index"
    });

    options.SwaggerDoc("v2", new Microsoft.OpenApi.OpenApiInfo
    {
        Version = "v2",
        Title = "EDDN Index Lookup",
        Description = "API for querying EDDN capture index"
    });

    options.IncludeXmlComments(Assembly.GetExecutingAssembly());
});

builder.Services.PostConfigure<ForwardedHeadersOptions>(opts =>
{
    var fwdhdrs = builder.Configuration.GetSection("ForwardedHeaders");

    foreach (string net in fwdhdrs.GetSection("KnownIPNetworks").Get<List<string>>() ?? [])
    {
        if (System.Net.IPNetwork.TryParse(net, out var ipnet))
        {
            opts.KnownIPNetworks.Add(ipnet);
        }
    }

    foreach (string proxy in fwdhdrs.GetSection("KnownProxies").Get<List<string>>() ?? [])
    {
        if (System.Net.IPAddress.TryParse(proxy, out var proxyIp))
        {
            opts.KnownProxies.Add(proxyIp);
        }
    }

    fwdhdrs.Bind(opts);
});

var app = builder.Build();

if (app.Configuration.GetValue<string>("ASPNETCORE_APPL_PATH") is string appPath && !string.IsNullOrWhiteSpace(appPath))
{
    if (app.Configuration.GetValue<string>("ASPNETCORE_APPL_HOST") is string appHost && !string.IsNullOrWhiteSpace(appHost))
    {
        app.UseWhen(ctx => !string.Equals(ctx.Request.Host.Host, appHost, StringComparison.OrdinalIgnoreCase), appNoHost => appNoHost.UsePathBase(appPath));
    }
    else
    {
        app.UsePathBase(appPath);
    }
}

app.UseSwagger();
app.MapSwagger("/openapi/{documentName}.{extension:regex(^(json|ya?ml)$)}");

app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("../openapi/v2.json", "EDDN Index Lookup v2");
    opts.SwaggerEndpoint("../openapi/v1.json", "EDDN Index Lookup v1 Backwards Compatibility Endpoint");
    opts.InjectStylesheet("custom.css");
});

app.MapScalarApiReference(opts =>
{
    opts.AddDocument("v2", "EDDN Index Lookup v2");
    opts.AddDocument("v1", "EDDN Index Lookup v1 Backwards Compatibility Endpoint");
    opts.DisableAgent();
    opts.DisableMcp();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
