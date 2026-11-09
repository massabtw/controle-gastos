using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Infrastructure.Data;

public class DatabaseMigrator
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(string connectionString, ILogger<DatabaseMigrator> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        try
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var statements = new[]
            {
                // FinancialProfile
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FinancialProfile') AND name = 'Uuid') ALTER TABLE dbo.FinancialProfile ADD Uuid UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FinancialProfile') AND name = 'CreatedAt') ALTER TABLE dbo.FinancialProfile ADD CreatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FinancialProfile') AND name = 'UpdatedAt') ALTER TABLE dbo.FinancialProfile ADD UpdatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FinancialProfile') AND name = 'Deleted') ALTER TABLE dbo.FinancialProfile ADD Deleted BIT NULL DEFAULT 0;",
                "EXEC('UPDATE dbo.FinancialProfile SET Deleted = 0 WHERE Deleted IS NULL;');",

                // FixedCommitments
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'Uuid') ALTER TABLE dbo.FixedCommitments ADD Uuid UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'CreatedAt') ALTER TABLE dbo.FixedCommitments ADD CreatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'UpdatedAt') ALTER TABLE dbo.FixedCommitments ADD UpdatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'Deleted') ALTER TABLE dbo.FixedCommitments ADD Deleted BIT NULL DEFAULT 0;",
                "EXEC('UPDATE dbo.FixedCommitments SET Deleted = 0 WHERE Deleted IS NULL;');",

                // CardExpenses
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CardExpenses') AND name = 'Uuid') ALTER TABLE dbo.CardExpenses ADD Uuid UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CardExpenses') AND name = 'CreatedAt') ALTER TABLE dbo.CardExpenses ADD CreatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CardExpenses') AND name = 'UpdatedAt') ALTER TABLE dbo.CardExpenses ADD UpdatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.CardExpenses') AND name = 'Deleted') ALTER TABLE dbo.CardExpenses ADD Deleted BIT NULL DEFAULT 0;",
                "EXEC('UPDATE dbo.CardExpenses SET Deleted = 0 WHERE Deleted IS NULL;');",

                // InvoicePayments
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoicePayments') AND name = 'Uuid') ALTER TABLE dbo.InvoicePayments ADD Uuid UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoicePayments') AND name = 'CreatedAt') ALTER TABLE dbo.InvoicePayments ADD CreatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoicePayments') AND name = 'UpdatedAt') ALTER TABLE dbo.InvoicePayments ADD UpdatedAt DATETIMEOFFSET(7) NULL DEFAULT (SYSDATETIMEOFFSET());",
                "IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.InvoicePayments') AND name = 'Deleted') ALTER TABLE dbo.InvoicePayments ADD Deleted BIT NULL DEFAULT 0;",
                "EXEC('UPDATE dbo.InvoicePayments SET Deleted = 0 WHERE Deleted IS NULL;');"
            };

            foreach (var stmt in statements)
            {
                await conn.ExecuteAsync(stmt);
            }
            _logger.LogInformation("Database migration completed successfully with standard columns.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run database migrations.");
        }
    }
}
