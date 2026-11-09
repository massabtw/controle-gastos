using FinanceTracker.Domain.Http.Groq.Models;

namespace FinanceTracker.Domain.Http.Groq;

public interface IGroqService
{
    Task<ParsedExpenseIntentModel?> ParseExpenseIntentAsync(string userMessage);
}
