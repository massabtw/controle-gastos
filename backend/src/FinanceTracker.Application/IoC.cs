using System.Globalization;
using FinanceTracker.Application.Interfaces.Commands;
using FinanceTracker.Application.Interfaces.Commitments;
using FinanceTracker.Application.Interfaces.Expenses;
using FinanceTracker.Application.Interfaces.FinancialEngine;
using FinanceTracker.Application.Interfaces.Integrations;
using FinanceTracker.Application.Interfaces.Invoices;
using FinanceTracker.Application.Interfaces.Profile;
using FinanceTracker.Application.Interfaces.System;
using FinanceTracker.Application.Services.Commands;
using FinanceTracker.Application.Services.Commitments;
using FinanceTracker.Application.Services.Expenses;
using FinanceTracker.Application.Services.FinancialEngine;
using FinanceTracker.Application.Services.Integrations;
using FinanceTracker.Application.Services.Invoices;
using FinanceTracker.Application.Services.Profile;
using FinanceTracker.Application.Services.System;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Application;

public static class IoC
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
        services.AddValidatorsFromAssembly(typeof(IoC).Assembly);

        services.AddTransient<IExpenseService, ExpenseService>();
        services.AddTransient<IFinancialEngineService, FinancialEngineService>();
        services.AddTransient<IFinancialProfileService, FinancialProfileService>();
        services.AddTransient<IFixedCommitmentService, FixedCommitmentService>();
        services.AddTransient<IInvoiceService, InvoiceService>();
        services.AddTransient<ICommandProcessorService, CommandProcessorService>();
        services.AddTransient<ISystemService, SystemService>();
        services.AddTransient<IIosIntegrationService, IosIntegrationService>();

        return services;
    }
}
