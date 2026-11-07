using System.Text.Json.Serialization;
using SupplierFeed.Api.Data;
using SupplierFeed.Api.Infrastructure;
using SupplierFeed.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
builder.Services.AddSingleton<IReservationIngestService, ReservationIngestService>();
builder.Services.AddSingleton<IReservationRepository, ReservationRepository>();
builder.Services.AddSingleton<ISupplierStatsRepository, SupplierStatsRepository>();
builder.Services.AddHostedService<DatabaseInitializer>();
builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var application = builder.Build();

application.UseSwagger();
application.UseSwaggerUI();
application.MapControllers();

application.Run();

public partial class Program;
