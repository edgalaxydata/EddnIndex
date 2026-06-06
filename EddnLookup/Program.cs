using EddnIndexUpdate;
using EddnLookup.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
                .AddJsonOptions(opts => opts.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddDbContextFactory<EddnIndexUpdate.Models.EDDNContext>(opts => opts.ConfigureDB(builder.Configuration.GetSection("Database")));
builder.Services.Configure<FileProcessorSettings>(builder.Configuration.GetSection("FileProcessor"));
builder.Services.AddTransient<APIService>();
builder.Services.AddSwaggerGen(options =>
{
    options.UseAllOfForInheritance();
    options.UseOneOfForPolymorphism();

    options.SwaggerDoc("v2", new Microsoft.OpenApi.OpenApiInfo
    {
        Version = "v2",
        Title = "EDDN Lookup",
        Description = "API for querying EDDN capture index"
    });

    options.IncludeXmlComments(Assembly.GetExecutingAssembly());
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(opts => opts.SwaggerEndpoint("/swagger/v2/swagger.json", "EDDNLookup-v2"));

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
