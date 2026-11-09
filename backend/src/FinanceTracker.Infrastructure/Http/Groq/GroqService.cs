using System.Text;
using System.Text.Json;
using FinanceTracker.Domain.Http.Groq;
using FinanceTracker.Domain.Http.Groq.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceTracker.Infrastructure.Http.Groq;

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqService> _logger;

    public GroqService(
        HttpClient httpClient,
        IOptions<GroqOptions> options,
        ILogger<GroqService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ParsedExpenseIntentModel?> ParseExpenseIntentAsync(string userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return null;
        }

        try
        {
            const string systemPrompt = @"Você é um assistente financeiro especialista em interpretar comandos e despesas em português brasileiro informal.
Sua tarefa é analisar a mensagem do usuário e retornar EXCLUSIVAMENTE um JSON com os seguintes campos:
- intent: 'ADD_EXPENSE' (para compras/gastos), 'RECEIVE_PIX' (quando o usuário recebe Pix/dinheiro externo/reembolso: 'recebi 50 pix do joao', 'recebi pix 30', 'reembolso 40'), 'DEPOSIT_CAIXINHA' (quando o usuário quer adicionar/guardar valor na Caixinha: '+10 caixinha', 'guardar 50 na caixinha', 'adicionar 100 na caixinha', 'botar 20 na caixinha'), 'PAY_INVOICE' (para 'paguei a fatura', 'fatura paga', 'quitar fatura'), 'INVEST_SURPLUS' (para 'investir sobra', 'guardar sobra'), 'SYNC_INVOICE' (ajustar fatura atual), 'UPDATE_CAIXINHA' (atualizar saldo total da Caixinha), 'SIMULATE' (para simular compras), 'STATUS' (para ver resumo/fatura), 'UNDO' (para desfazer último lançamento), ou 'UNKNOWN' (se não fizer sentido).
- description: Título limpo e capitalizado do gasto ou comando.
- amount: Valor numérico em reais (ex: 25.00, 150.50, 350.0, 4500.0).
- installments: Quantidade de parcelas (inteiro, padrão 1 se for à vista).
- category: Escolha exatamente uma entre: 'Alimentação', 'Transporte', 'Lazer', 'Compras', 'Assinaturas', 'Saúde', 'Entrada', 'Outros'.
- paymentMethod: 'PIX_INFLOW' se for recebimento/reembolso; 'PIX_CAIXINHA' se o usuário mencionar 'pix', 'caixinha', 'retirar da caixinha', 'pagar no pix'; caso contrário 'CARD'.
- transactionDate: Se o usuário mencionar 'ontem' ou uma data específica, retorne em formato 'YYYY-MM-DD', senão null.";

            var requestModel = new GroqChatRequestModel
            {
                Model = !string.IsNullOrWhiteSpace(_options.Model) ? _options.Model : "llama-3.3-70b-versatile",
                Temperature = 0.1,
                Messages = new List<GroqMessageModel>
                {
                    new() { Role = "system", Content = systemPrompt },
                    new() { Role = "user", Content = userMessage }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestModel), Encoding.UTF8, "application/json");
            var uri = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";

            var response = await _httpClient.PostAsync(uri, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Groq API returned status {StatusCode}: {Error}", response.StatusCode, err);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<GroqChatResponseModel>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var messageContent = chatResponse?.Choices.FirstOrDefault()?.Message.Content;
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return null;
            }

            var parsed = JsonSerializer.Deserialize<ParsedExpenseIntentModel>(messageContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Groq API");
            return null;
        }
    }
}
