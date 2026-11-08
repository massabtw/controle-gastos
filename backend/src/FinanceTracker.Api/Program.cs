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

app.Run();
