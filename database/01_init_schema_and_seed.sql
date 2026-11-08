-- ====================================================================
-- Script: 01_init_schema_and_seed.sql
-- Description: Idempotent initialization of FinanceTrackerDb and tables
-- ====================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'FinanceTrackerDb')
BEGIN
    CREATE DATABASE [FinanceTrackerDb];
END
GO

USE [FinanceTrackerDb];
GO

-- 1. Table: FinancialProfile
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'FinancialProfile' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[FinancialProfile] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [IncomeFirstTranche] DECIMAL(10,2) NOT NULL DEFAULT 1200.00,
        [IncomeSecondTranche] DECIMAL(10,2) NOT NULL DEFAULT 1000.00,
        [MonthlyInvestmentGoal] DECIMAL(10,2) NOT NULL DEFAULT 1000.00,
        [ClosingDay] INT NOT NULL DEFAULT 8,
        [DueDay] INT NOT NULL DEFAULT 16,
        [CdiAnnualRate] DECIMAL(5,2) NOT NULL DEFAULT 11.25,
        [NubankCaixinhaBalance] DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        [NubankCaixinhaCdiRate] DECIMAL(5,2) NOT NULL DEFAULT 115.00,
        [GroqApiKey] NVARCHAR(200) NULL,
        [TelegramBotToken] NVARCHAR(200) NULL,
        [TelegramAllowedChatId] BIGINT NULL
    );
END
GO

-- 2. Table: FixedCommitments
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'FixedCommitments' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[FixedCommitments] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [Title] NVARCHAR(100) NOT NULL,
        [Amount] DECIMAL(10,2) NOT NULL,
        [StartCycle] CHAR(7) NOT NULL, -- Format 'YYYY-MM'
        [TotalInstallments] INT NULL,   -- NULL = recurring indefinitely; INT = fixed duration in months
        [IsActive] BIT NOT NULL DEFAULT 1
    );
END
GO

-- 3. Table: CardExpenses
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = N'CardExpenses' AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[CardExpenses] (
        [Id] INT PRIMARY KEY IDENTITY(1,1),
        [Description] NVARCHAR(150) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [Amount] DECIMAL(10,2) NOT NULL,
        [TransactionDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [BillingCycle] CHAR(7) NOT NULL, -- Format 'YYYY-MM'
        [InstallmentCurrent] INT NOT NULL DEFAULT 1,
        [InstallmentTotal] INT NOT NULL DEFAULT 1,
        [PurchaseGroupId] UNIQUEIDENTIFIER NULL,
        [PaymentMethod] NVARCHAR(20) NOT NULL DEFAULT 'CARD',
        [DeductFromCycleCap] BIT NOT NULL DEFAULT 1
    );

    CREATE INDEX IX_CardExpenses_BillingCycle ON [dbo].[CardExpenses]([BillingCycle]);
    CREATE INDEX IX_CardExpenses_PurchaseGroupId ON [dbo].[CardExpenses]([PurchaseGroupId]);
END
GO

-- 4. Initial Seed Data
IF NOT EXISTS (SELECT 1 FROM [dbo].[FinancialProfile])
BEGIN
    INSERT INTO [dbo].[FinancialProfile] 
        ([IncomeFirstTranche], [IncomeSecondTranche], [MonthlyInvestmentGoal], [ClosingDay], [DueDay], [CdiAnnualRate])
    VALUES 
        (1200.00, 1000.00, 1000.00, 8, 16, 11.25);
END
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[FixedCommitments])
BEGIN
    INSERT INTO [dbo].[FixedCommitments] 
        ([Title], [Amount], [StartCycle], [TotalInstallments], [IsActive])
    VALUES 
        (N'Internet Móvel Mãe', 50.00, '2026-12', NULL, 1),
        (N'Monitor Mãe', 120.00, '2026-12', 5, 1);
END
GO
