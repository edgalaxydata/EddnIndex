using EddnIndexUpdate;
using EddnIndexLookup.Services;
using System.Reflection;
using EddnIndexUpdate.Options;
using EddnIndexLookup.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Configuration.AddJsonFile("appsettings.json", optional: true);
builder.Configuration.AddJsonFile("hosting.json", optional: true);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

builder.Services.AddControllers()
                .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDbContextFactory<EddnIndexUpdate.Models.EDDNContext>(opts => opts.ConfigureDB(builder.Configuration.GetSection("Database")));
builder.Services.Configure<EddnLookupServiceSettings>(builder.Configuration.GetSection("APIService"));
builder.Services.AddTransient<EddnLookupService>();
builder.Services.AddSwaggerGen(options =>
{
    options.UseAllOfForInheritance();
    options.UseOneOfForPolymorphism();

    options.SwaggerDoc("v2", new Microsoft.OpenApi.OpenApiInfo
    {
        Version = "v2",
        Title = "EDDN Index Lookup",
        Description = "API for querying EDDN capture index"
    });

    options.IncludeXmlComments(Assembly.GetExecutingAssembly());
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v2/swagger.json", "EDDN Index Lookup"));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
