using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using SupplierFeed.Api.Data;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Tests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    #region Properties
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));

    public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), $"supplier-feed-tests-{Guid.NewGuid():N}.db");
    #endregion

    #region Public Methods
    public async Task<long> CountReservationsAsync(string supplierId, string reservationId)
    {
        await using var connection = await Services.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync(CancellationToken.None);

        return await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM reservations WHERE supplier_id = @SupplierId AND reservation_id = @ReservationId;",
            new { SupplierId = supplierId, ReservationId = reservationId });
    }

    public async Task<Reservation?> FindReservationAsync(string supplierId, string reservationId)
    {
        await using var connection = await Services.GetRequiredService<IDbConnectionFactory>().OpenConnectionAsync(CancellationToken.None);

        return await Services
            .GetRequiredService<IReservationRepository>()
            .GetByCompositeKeyAsync(connection, supplierId, reservationId, CancellationToken.None);
    }
    #endregion

    #region Overrides
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:DatabasePath", DatabasePath);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();

        foreach (var databaseFilePath in new[] { DatabasePath, $"{DatabasePath}-shm", $"{DatabasePath}-wal" })
        {
            File.Delete(databaseFilePath);
        }
    }
    #endregion
}
