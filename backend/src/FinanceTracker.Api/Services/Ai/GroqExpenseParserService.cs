using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinanceTracker.Api.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceTracker.Api.Services.Ai;

public interface IGroqExpenseParserService
{
    Task<ParsedExpenseIntent> ParseAsync(string message);
}

public class GroqExpenseParserService : IGroqExpenseParserService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<GroqExpenseParserService> _logger;

    public GroqExpenseParserService(
        HttpClient httpClient,
        IConfiguration configuration,
        IProfileRepository profileRepository,
        ILogger<GroqExpenseParserService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<ParsedExpenseIntent> ParseAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new ParsedExpenseIntent { Intent = "UNKNOWN" };
        }

        var profile = await _profileRepository.GetProfileAsync();
        var apiKey = !string.IsNullOrWhiteSpace(profile?.GroqApiKey)
            ? profile.GroqApiKey
            : _configuration["Groq:ApiKey"];

        var model = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";

        // Fallback directly if no API key is provided
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("Groq API key is not configured. Using deterministic regex parser fallback.");
            return RegexExpenseParser.Parse(message);
        }

        try
        {
            var systemPrompt = @"Você é um assistente financeiro especialista em interpretar comandos e despesas em português brasileiro informal.
Sua tarefa é analisar a mensagem do usuário e retornar EXCLUSIVAMENTE um JSON com os seguintes campos:
- intent: 'ADD_EXPENSE' (para compras/gastos), 'SIMULATE' (para simular compras), 'STATUS' (para ver resumo/fatura), 'UNDO' (para desfazer último lançamento), ou 'UNKNOWN' (se não fizer sentido).
- description: Título limpo e capitalizado do gasto (ex: 'Uber', 'Almoço no Shopping', 'Barbearia', 'Lanche', 'Pix Caixinha').
- amount: Valor numérico em reais (ex: 25.00, 150.50).
- installments: Quantidade de parcelas (inteiro, padrão 1 se for à vista).
- category: Escolha exatamente uma entre: 'Alimentação', 'Transporte', 'Lazer', 'Compras', 'Assinaturas', 'Saúde', 'Outros'.
- paymentMethod: 'PIX_CAIXINHA' se o usuário mencionar 'pix', 'caixinha', 'retirar da caixinha', 'pagar no pix'; caso contrário 'CARD'.
- transactionDate: Se o usuário mencionar 'ontem' ou uma data específica, retorne em formato 'YYYY-MM-DD', senão null.

Exemplos:
'uber 25' -> {'intent': 'ADD_EXPENSE', 'description': 'Uber', 'amount': 25.0, 'installments': 1, 'category': 'Transporte', 'paymentMethod': 'CARD', 'transactionDate': null}
'pix 40 barbearia' -> {'intent': 'ADD_EXPENSE', 'description': 'Barbearia', 'amount': 40.0, 'installments': 1, 'category': 'Saúde', 'paymentMethod': 'PIX_CAIXINHA', 'transactionDate': null}
'paguei 25 no pix lanche' -> {'intent': 'ADD_EXPENSE', 'description': 'Lanche', 'amount': 25.0, 'installments': 1, 'category': 'Alimentação', 'paymentMethod': 'PIX_CAIXINHA', 'transactionDate': null}
'tirei 100 da caixinha pro pix' -> {'intent': 'ADD_EXPENSE', 'description': 'Pix Caixinha', 'amount': 100.0, 'installments': 1, 'category': 'Outros', 'paymentMethod': 'PIX_CAIXINHA', 'transactionDate': null}
'comprei um jogo 150 em 3x' -> {'intent': 'ADD_EXPENSE', 'description': 'Jogo', 'amount': 150.0, 'installments': 3, 'category': 'Lazer', 'paymentMethod': 'CARD', 'transactionDate': null}
'simular 400 4x' -> {'intent': 'SIMULATE', 'description': 'Simulação', 'amount': 400.0, 'installments': 4, 'category': 'Compras', 'paymentMethod': 'CARD', 'transactionDate': null}
'/status' -> {'intent': 'STATUS', 'description': '', 'amount': 0.0, 'installments': 1, 'category': 'Outros', 'paymentMethod': 'CARD', 'transactionDate': null}
'/desfazer' -> {'intent': 'UNDO', 'description': '', 'amount': 0.0, 'installments': 1, 'category': 'Outros', 'paymentMethod': 'CARD', 'transactionDate': null}
";

            var requestBody = new
            {
                model = model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = message }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1
            };

            var requestJson = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Groq API returned status {StatusCode}: {Error}. Falling back to Regex parser.", response.StatusCode, errorContent);
                return RegexExpenseParser.Parse(message);
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var contentString = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(contentString))
            {
                return RegexExpenseParser.Parse(message);
            }

            var parsed = JsonSerializer.Deserialize<ParsedExpenseIntent>(contentString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return parsed ?? RegexExpenseParser.Parse(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Groq API. Falling back to Regex parser.");
            return RegexExpenseParser.Parse(message);
        }
    }
}
