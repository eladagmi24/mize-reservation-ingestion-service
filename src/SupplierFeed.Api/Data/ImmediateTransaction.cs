using System.Data.Common;
using Dapper;

namespace SupplierFeed.Api.Data;

public sealed class ImmediateTransaction : IAsyncDisposable
{
    #region Fields & Dependencies
    private readonly DbConnection _connection;
    private bool _isCompleted;
    #endregion

    #region Constructor
    private ImmediateTransaction(DbConnection connection)
    {
        _connection = connection;
    }
    #endregion

    #region Public Methods
    public static async Task<ImmediateTransaction> BeginAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition("BEGIN IMMEDIATE;", cancellationToken: cancellationToken));
        return new ImmediateTransaction(connection);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        await _connection.ExecuteAsync(new CommandDefinition("COMMIT;", cancellationToken: cancellationToken));
        _isCompleted = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_isCompleted)
        {
            return;
        }

        _isCompleted = true;
        await _connection.ExecuteAsync("ROLLBACK;");
    }
    #endregion
}
