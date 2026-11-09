using Npgsql;

namespace FinanceTracker.Infrastructure.Data;

public static class ConnectionStringParser
{
    public static (string ConnectionString, bool IsPostgres) Resolve(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (string.Empty, false);
        }

        var trimmed = raw.Trim();

        // 1. Check for postgres:// or postgresql:// URL format
        if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(trimmed);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? userInfo[0] : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = SslMode.Require,
                Pooling = true,
                Timeout = 15,
                CommandTimeout = 30
            };

            return (builder.ConnectionString, true);
        }

        // 2. Check if it's already an Npgsql key-value connection string (e.g., Host=...;Username=...)
        if (trimmed.Contains("Host=", StringComparison.OrdinalIgnoreCase) &&
            (trimmed.Contains("Username=", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("User ID=", StringComparison.OrdinalIgnoreCase)))
        {
            var builder = new NpgsqlConnectionStringBuilder(trimmed);
            if (!builder.ContainsKey("SslMode"))
            {
                builder.SslMode = SslMode.Require;
            }
            return (builder.ConnectionString, true);
        }

        // 3. Fallback to SQL Server
        return (trimmed, false);
    }
}
