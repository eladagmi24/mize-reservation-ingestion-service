using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SupplierFeed.Api.Data;
using SupplierFeed.Api.Infrastructure;

namespace SupplierFeed.Tests;

public sealed class DatabaseInitializerTests : IDisposable
{
    #region Fields & Dependencies
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly string _databasePath;
    private readonly DatabaseInitializer _initializer;
    #endregion

    #region Constructor
    public DatabaseInitializerTests()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"supplier-feed-{Guid.NewGuid():N}.db");
        _connectionFactory = new SqliteConnectionFactory(Options.Create(new DatabaseOptions { DatabasePath = _databasePath }));
        _initializer = new DatabaseInitializer(_connectionFactory);
    }
    #endregion

    #region Public Methods
    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        foreach (var databaseFilePath in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
        {
            File.Delete(databaseFilePath);
        }
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_DoesNotFail()
    {
        await _initializer.InitializeAsync(CancellationToken.None);

        var secondRun = () => _initializer.InitializeAsync(CancellationToken.None);

        await secondRun.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_CreatesReservationsAndSupplierStatsTables()
    {
        await _initializer.InitializeAsync(CancellationToken.None);

        await using var connection = await _connectionFactory.OpenConnectionAsync(CancellationToken.None);
        var tableNames = await connection.QueryAsync<string>("SELECT name FROM sqlite_master WHERE type = 'table';");

        tableNames.Should().Contain(new[] { "reservations", "supplier_stats" });
    }

    [Fact]
    public async Task InitializeAsync_EnablesWriteAheadLogging()
    {
        await _initializer.InitializeAsync(CancellationToken.None);

        await using var connection = await _connectionFactory.OpenConnectionAsync(CancellationToken.None);
        var journalMode = await connection.QuerySingleAsync<string>("PRAGMA journal_mode;");

        journalMode.Should().Be("wal");
    }

    [Fact]
    public async Task OpenConnectionAsync_AppliesConfiguredBusyTimeout()
    {
        await _initializer.InitializeAsync(CancellationToken.None);

        await using var connection = await _connectionFactory.OpenConnectionAsync(CancellationToken.None);
        var busyTimeout = await connection.QuerySingleAsync<int>("PRAGMA busy_timeout;");

        busyTimeout.Should().Be(new DatabaseOptions().BusyTimeoutMilliseconds);
    }

    [Fact]
    public async Task ReservationsTable_WhenSupplierAndReservationRepeat_RejectsDuplicateKey()
    {
        await _initializer.InitializeAsync(CancellationToken.None);
        await using var connection = await _connectionFactory.OpenConnectionAsync(CancellationToken.None);
        const string insertCommand = """
            INSERT INTO reservations (supplier_id, reservation_id, room_id, check_in_utc, check_out_utc, price, payload_hash, updated_at_utc, first_seen_utc, last_modified_utc)
            VALUES ('supplier-1', 'reservation-1', 'room-1', '2026-08-01T00:00:00Z', '2026-08-03T00:00:00Z', '450', x'00', '2026-07-01T10:15:00Z', '2026-07-01T10:15:00Z', '2026-07-01T10:15:00Z');
            """;
        await connection.ExecuteAsync(insertCommand);

        var duplicateInsert = () => connection.ExecuteAsync(insertCommand);

        await duplicateInsert.Should().ThrowAsync<SqliteException>();
    }
    #endregion
}
