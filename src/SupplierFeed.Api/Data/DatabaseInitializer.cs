using Dapper;

namespace SupplierFeed.Api.Data;

public sealed class DatabaseInitializer : IHostedService
{
    #region Constants
    private const string CreateSchemaCommand = """
        CREATE TABLE IF NOT EXISTS reservations (
            supplier_id       TEXT    NOT NULL,
            reservation_id    TEXT    NOT NULL,
            room_id           TEXT    NOT NULL,
            check_in_utc      TEXT    NOT NULL,
            check_out_utc     TEXT    NOT NULL,
            price             TEXT    NOT NULL,
            payload_hash      BLOB    NOT NULL,
            updated_at_utc    TEXT    NOT NULL,
            first_seen_utc    TEXT    NOT NULL,
            last_modified_utc TEXT    NOT NULL,
            version           INTEGER NOT NULL DEFAULT 1,
            PRIMARY KEY (supplier_id, reservation_id)
        ) WITHOUT ROWID;

        CREATE TABLE IF NOT EXISTS supplier_stats (
            supplier_id      TEXT    PRIMARY KEY,
            ingested_count   INTEGER NOT NULL DEFAULT 0,
            throttled_count  INTEGER NOT NULL DEFAULT 0,
            created_count    INTEGER NOT NULL DEFAULT 0,
            updated_count    INTEGER NOT NULL DEFAULT 0,
            duplicate_count  INTEGER NOT NULL DEFAULT 0,
            stale_count      INTEGER NOT NULL DEFAULT 0,
            conflict_count   INTEGER NOT NULL DEFAULT 0,
            last_request_utc TEXT
        ) WITHOUT ROWID;
        """;

    private const string EnableWriteAheadLoggingCommand = "PRAGMA journal_mode = WAL;";
    #endregion

    #region Fields & Dependencies
    private readonly IDbConnectionFactory _connectionFactory;
    #endregion

    #region Constructor
    public DatabaseInitializer(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }
    #endregion

    #region Public Methods
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(EnableWriteAheadLoggingCommand, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(CreateSchemaCommand, cancellationToken: cancellationToken));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    #endregion
}
