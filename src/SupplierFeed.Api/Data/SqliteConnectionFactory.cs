using System.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using SupplierFeed.Api.Infrastructure;

namespace SupplierFeed.Api.Data;

public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
    #region Fields & Dependencies
    private readonly int _busyTimeoutMilliseconds;
    private readonly string _connectionString;
    #endregion

    #region Constructor
    public SqliteConnectionFactory(IOptions<DatabaseOptions> databaseOptions)
    {
        _busyTimeoutMilliseconds = databaseOptions.Value.BusyTimeoutMilliseconds;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databaseOptions.Value.DatabasePath,
            ForeignKeys = true,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true
        }.ToString();
    }
    #endregion

    #region Public Methods
    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            await ApplyConnectionPragmasAsync(connection, cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
    #endregion

    #region Private Helpers
    private async Task ApplyConnectionPragmasAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var pragmaCommand = $"PRAGMA busy_timeout = {_busyTimeoutMilliseconds}; PRAGMA synchronous = NORMAL;";
        await connection.ExecuteAsync(new CommandDefinition(pragmaCommand, cancellationToken: cancellationToken));
    }
    #endregion
}
