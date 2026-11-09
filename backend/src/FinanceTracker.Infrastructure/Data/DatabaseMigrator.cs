using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FinanceTracker.Infrastructure.Data;

public class DatabaseMigrator
{
    private readonly string _connectionString;
    private readonly bool _isPostgres;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(string rawConnectionString, ILogger<DatabaseMigrator> logger)
    {
        var (resolved, isPg) = ConnectionStringParser.Resolve(rawConnectionString);
        _connectionString = resolved;
        _isPostgres = isPg;
        _logger = logger;
    }

    public async Task MigrateAsync()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogWarning("No connection string provided for migrations.");
            return;
        }

        try
        {
            if (_isPostgres)
            {
                await MigratePostgresAsync();
            }
            else
            {
                await MigrateSqlServerAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run database migrations.");
        }
    }

    private async Task MigratePostgresAsync()
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        const string ddl = @"
            CREATE TABLE IF NOT EXISTS FinancialProfile (
                Id SERIAL PRIMARY KEY,
                Uuid UUID NOT NULL DEFAULT gen_random_uuid(),
                IncomeFirstTranche NUMERIC(12,2) NOT NULL DEFAULT 1200.00,
                IncomeSecondTranche NUMERIC(12,2) NOT NULL DEFAULT 1000.00,
                MonthlyInvestmentGoal NUMERIC(12,2) NOT NULL DEFAULT 1000.00,
                ClosingDay INT NOT NULL DEFAULT 8,
                DueDay INT NOT NULL DEFAULT 16,
                CdiAnnualRate NUMERIC(6,2) NOT NULL DEFAULT 11.25,
                NubankCaixinhaBalance NUMERIC(12,2) NOT NULL DEFAULT 4935.79,
                NubankCaixinhaCdiRate NUMERIC(6,2) NOT NULL DEFAULT 115.00,
                CreditCardLimit NUMERIC(12,2) NOT NULL DEFAULT 2500.00,
                CustomCardCap NUMERIC(12,2) NULL,
                GroqApiKey TEXT NULL,
                TelegramBotToken TEXT NULL,
                TelegramAllowedChatId BIGINT NULL,
                CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Deleted BOOLEAN NOT NULL DEFAULT FALSE
            );

            CREATE TABLE IF NOT EXISTS FixedCommitments (
                Id SERIAL PRIMARY KEY,
                Uuid UUID NOT NULL DEFAULT gen_random_uuid(),
                Title VARCHAR(150) NOT NULL,
                Amount NUMERIC(12,2) NOT NULL,
                StartCycle VARCHAR(7) NOT NULL,
                TotalInstallments INT NULL,
                IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                CommitmentType VARCHAR(20) NOT NULL DEFAULT 'PIX_COMMITMENT',
                PaymentSource VARCHAR(20) NOT NULL DEFAULT 'PIX_CAIXINHA',
                BillingDay INT NULL DEFAULT 1,
                LastPaidCycle VARCHAR(7) NULL,
                CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Deleted BOOLEAN NOT NULL DEFAULT FALSE
            );

            CREATE TABLE IF NOT EXISTS CardExpenses (
                Id SERIAL PRIMARY KEY,
                Uuid UUID NOT NULL DEFAULT gen_random_uuid(),
                Description VARCHAR(200) NOT NULL,
                Category VARCHAR(50) NOT NULL DEFAULT 'Outros',
                Amount NUMERIC(12,2) NOT NULL,
                TransactionDate TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                BillingCycle VARCHAR(7) NOT NULL,
                InstallmentCurrent INT NOT NULL DEFAULT 1,
                InstallmentTotal INT NOT NULL DEFAULT 1,
                PurchaseGroupId UUID NULL,
                PaymentMethod VARCHAR(20) NOT NULL DEFAULT 'CARD',
                DeductFromCycleCap BOOLEAN NOT NULL DEFAULT TRUE,
                CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Deleted BOOLEAN NOT NULL DEFAULT FALSE
            );

            CREATE TABLE IF NOT EXISTS InvoicePayments (
                Id SERIAL PRIMARY KEY,
                Uuid UUID NOT NULL DEFAULT gen_random_uuid(),
                BillingCycle VARCHAR(7) NOT NULL UNIQUE,
                IsPaid BOOLEAN NOT NULL DEFAULT TRUE,
                PaidAmount NUMERIC(12,2) NOT NULL DEFAULT 0.00,
                PaidAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                SurplusAmount NUMERIC(12,2) NOT NULL DEFAULT 0.00,
                SurplusInvested BOOLEAN NOT NULL DEFAULT FALSE,
                Notes TEXT NULL,
                CreatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                UpdatedAt TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                Deleted BOOLEAN NOT NULL DEFAULT FALSE
            );
        ";

        await conn.ExecuteAsync(ddl);

        // Seed initial profile if empty
        var profileCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM FinancialProfile;");
        if (profileCount == 0)
        {
            const string seedProfileSql = @"
                INSERT INTO FinancialProfile (
                    IncomeFirstTranche, IncomeSecondTranche, MonthlyInvestmentGoal,
                    ClosingDay, DueDay, CdiAnnualRate, NubankCaixinhaBalance, NubankCaixinhaCdiRate,
                    CreditCardLimit
                ) VALUES (
                    1200.00, 1000.00, 1000.00, 8, 16, 11.25, 4935.79, 115.00, 2500.00
                );";
            await conn.ExecuteAsync(seedProfileSql);
        }

        // Seed initial commitments if empty
        var commitmentCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM FixedCommitments;");
        if (commitmentCount == 0)
        {
            const string seedCommitmentSql = @"
                INSERT INTO FixedCommitments (Title, Amount, StartCycle, TotalInstallments, IsActive, CommitmentType, PaymentSource, BillingDay)
                VALUES 
                    ('Internet Móvel Mãe', 50.00, '2026-12', NULL, TRUE, 'PIX_COMMITMENT', 'PIX_CAIXINHA', 10),
                    ('Monitor Mãe', 120.00, '2026-12', 5, TRUE, 'PIX_COMMITMENT', 'PIX_CAIXINHA', 15);";
            await conn.ExecuteAsync(seedCommitmentSql);
        }

        _logger.LogInformation("PostgreSQL (Neon) database auto-migration and verification completed successfully.");
    }

    private async Task MigrateSqlServerAsync()
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

        _logger.LogInformation("SQL Server database migration completed successfully with standard columns.");
    }
}
