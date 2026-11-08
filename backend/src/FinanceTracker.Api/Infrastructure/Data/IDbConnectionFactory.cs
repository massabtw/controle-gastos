using System.Data;

namespace FinanceTracker.Api.Infrastructure.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
