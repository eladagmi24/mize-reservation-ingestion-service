using Dapper;
using Microsoft.Extensions.Options;
using SupplierFeed.Api.Data;
using SupplierFeed.Api.Infrastructure;

namespace SupplierFeed.Api.Services;

public sealed class SqliteRateLimiter : IRateLimiter
{
    #region Constants
    private const string CountAdmissionsQuery = "SELECT COUNT(*) FROM supplier_request_admissions WHERE supplier_id = @SupplierId;";

    private const string DeleteExpiredAdmissionsCommand = "DELETE FROM supplier_request_admissions WHERE supplier_id = @SupplierId AND admitted_at_utc <= @WindowCutoffUtc;";

    private const string InsertAdmissionCommand = "INSERT INTO supplier_request_admissions (supplier_id, admitted_at_utc) VALUES (@SupplierId, @NowUtc);";

    private const string OldestAdmissionQuery = "SELECT MIN(admitted_at_utc) FROM supplier_request_admissions WHERE supplier_id = @SupplierId;";
    #endregion

    #region Fields & Dependencies
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly int _requestLimit;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _window;
    #endregion

    #region Constructor
    public SqliteRateLimiter(
        IDbConnectionFactory connectionFactory,
        IOptions<RateLimitOptions> rateLimitOptions,
        TimeProvider timeProvider)
    {
        _connectionFactory = connectionFactory;
        _requestLimit = rateLimitOptions.Value.RequestLimit;
        _timeProvider = timeProvider;
        _window = TimeSpan.FromSeconds(rateLimitOptions.Value.WindowSeconds);
    }
    #endregion

    #region Public Methods
    public async Task<RateLimitResult> TryAcquireAsync(string supplierId, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var parameters = new
        {
            NowUtc = SqliteTimestamp.ToText(now),
            SupplierId = supplierId,
            WindowCutoffUtc = SqliteTimestamp.ToText(now - _window)
        };

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await ImmediateTransaction.BeginAsync(connection, cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(DeleteExpiredAdmissionsCommand, parameters, cancellationToken: cancellationToken));
        var admissionCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(CountAdmissionsQuery, parameters, cancellationToken: cancellationToken));

        RateLimitResult result;
        if (admissionCount >= _requestLimit)
        {
            var oldestText = await connection.ExecuteScalarAsync<string>(new CommandDefinition(OldestAdmissionQuery, parameters, cancellationToken: cancellationToken));
            var oldestAdmission = SqliteTimestamp.ToUtcDateTime(oldestText!);
            result = RateLimitResult.Rejected(oldestAdmission + _window - now);
        }
        else
        {
            await connection.ExecuteAsync(new CommandDefinition(InsertAdmissionCommand, parameters, cancellationToken: cancellationToken));
            result = RateLimitResult.Admitted();
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }
    #endregion
}
