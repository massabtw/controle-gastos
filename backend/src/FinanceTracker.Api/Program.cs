using FinanceTracker.Api.Infrastructure.Data;
using FinanceTracker.Api.Infrastructure.Repositories;
using FinanceTracker.Api.Services;
using FinanceTracker.Api.Services.Ai;
using FinanceTracker.Api.Services.Telegram;

var builder = WebApplication.CreateBuilder(args);

// Controllers & JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Infrastructure & Repositories
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IExpenseRepository, ExpenseRepository>();

// Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IFinancialEngineService, FinancialEngineService>();
builder.Services.AddScoped<IGroqExpenseParserService, GroqExpenseParserService>();
builder.Services.AddScoped<ICommandProcessorService, CommandProcessorService>();
builder.Services.AddScoped<ITelegramBotService, TelegramBotService>();
builder.Services.AddHostedService<TelegramPollingBackgroundService>();

// CORS configuration for Frontend (Next.js localhost:3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Self-healing database check (UTF-8 encoding and schema validation)
try
{
    using var scope = app.Services.CreateScope();
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    using var connection = dbFactory.CreateConnection();
    Dapper.SqlMapper.Execute(connection, @"
        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FinancialProfile') AND name = 'CreditCardLimit')
            ALTER TABLE dbo.FinancialProfile ADD CreditCardLimit DECIMAL(12,2) NOT NULL DEFAULT 2500.00;

        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FinancialProfile') AND name = 'CustomCardCap')
            ALTER TABLE dbo.FinancialProfile ADD CustomCardCap DECIMAL(12,2) NULL;

        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'CommitmentType')
            ALTER TABLE dbo.FixedCommitments ADD CommitmentType NVARCHAR(20) NOT NULL DEFAULT 'PIX_COMMITMENT';

        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'PaymentSource')
            ALTER TABLE dbo.FixedCommitments ADD PaymentSource NVARCHAR(20) NOT NULL DEFAULT 'PIX_CAIXINHA';

        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'BillingDay')
            ALTER TABLE dbo.FixedCommitments ADD BillingDay INT NULL DEFAULT 1;

        IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.FixedCommitments') AND name = 'LastPaidCycle')
            ALTER TABLE dbo.FixedCommitments ADD LastPaidCycle CHAR(7) NULL;

        UPDATE dbo.FixedCommitments SET Title = N'Internet Móvel Mãe' WHERE Title LIKE '%Internet%';
        UPDATE dbo.FixedCommitments SET Title = N'Monitor Mãe' WHERE Title LIKE '%Monitor%';
        UPDATE dbo.FixedCommitments SET CommitmentType = 'PIX_COMMITMENT', PaymentSource = 'PIX_CAIXINHA' WHERE CommitmentType IS NULL OR CommitmentType = '';

        -- Ensure Internet Móvel Mãe exists as PIX_COMMITMENT
        IF NOT EXISTS (SELECT 1 FROM dbo.FixedCommitments WHERE Title LIKE N'%Internet%M%e%')
        BEGIN
            INSERT INTO dbo.FixedCommitments (Title, Amount, StartCycle, TotalInstallments, IsActive, CommitmentType, PaymentSource, BillingDay, LastPaidCycle)
            VALUES (N'Internet Móvel Mãe', 50.00, '2026-12', NULL, 1, 'PIX_COMMITMENT', 'PIX_CAIXINHA', 10, NULL);
        END
        ELSE
        BEGIN
            UPDATE dbo.FixedCommitments 
            SET IsActive = 1, CommitmentType = 'PIX_COMMITMENT', PaymentSource = 'PIX_CAIXINHA', Amount = 50.00
            WHERE Title LIKE N'%Internet%M%e%';
        END

        -- Ensure Monitor Mãe exists as PIX_COMMITMENT
        IF NOT EXISTS (SELECT 1 FROM dbo.FixedCommitments WHERE Title LIKE N'%Monitor%M%e%')
        BEGIN
            INSERT INTO dbo.FixedCommitments (Title, Amount, StartCycle, TotalInstallments, IsActive, CommitmentType, PaymentSource, BillingDay, LastPaidCycle)
            VALUES (N'Monitor Mãe', 120.00, '2026-12', 5, 1, 'PIX_COMMITMENT', 'PIX_CAIXINHA', 15, NULL);
        END
        ELSE
        BEGIN
            UPDATE dbo.FixedCommitments 
            SET IsActive = 1, CommitmentType = 'PIX_COMMITMENT', PaymentSource = 'PIX_CAIXINHA', Amount = 120.00
            WHERE Title LIKE N'%Monitor%M%e%';
        END
    ");
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Could not run self-healing DB check.");
}

app.Run();
