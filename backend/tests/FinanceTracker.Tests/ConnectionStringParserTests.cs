using FinanceTracker.Infrastructure.Data;
using Xunit;

namespace FinanceTracker.Tests;

public class ConnectionStringParserTests
{
    [Fact]
    public void Resolve_PostgreUri_ShouldParseSuccessfully()
    {
        var rawUri = "postgresql://alex:supersecret@ep-cool-butterfly-123456.us-east-2.aws.neon.tech/neondb?sslmode=require";
        var (connStr, isPostgres) = ConnectionStringParser.Resolve(rawUri);

        Assert.True(isPostgres);
        Assert.Contains("Host=ep-cool-butterfly-123456.us-east-2.aws.neon.tech", connStr);
        Assert.Contains("Database=neondb", connStr);
        Assert.Contains("Username=alex", connStr);
        Assert.Contains("Password=supersecret", connStr);
        Assert.Contains("SSL Mode=Require", connStr);
    }

    [Fact]
    public void Resolve_SqlServerConnStr_ShouldReturnIsPostgresFalse()
    {
        var rawSql = "Server=localhost\\SQLEXPRESS;Database=FinanceTrackerDb;Trusted_Connection=True;";
        var (connStr, isPostgres) = ConnectionStringParser.Resolve(rawSql);

        Assert.False(isPostgres);
        Assert.Equal(rawSql, connStr);
    }
}
