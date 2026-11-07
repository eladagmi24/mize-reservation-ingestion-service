using System.Data.Common;

namespace SupplierFeed.Api.Data;

public interface IDbConnectionFactory
{
    #region Operations
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
    #endregion
}
