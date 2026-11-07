using System.Text.Json.Serialization;
using SupplierFeed.Api.Data;
using SupplierFeed.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var application = builder.Build();

application.MapControllers();

application.Run();

public partial class Program;
