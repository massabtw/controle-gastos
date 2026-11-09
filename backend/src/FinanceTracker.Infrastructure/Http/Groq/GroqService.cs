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
        var apiKey = !string.IsNullOrWhiteSpace(_options.ApiKey)
            ? _options.ApiKey
            : Environment.GetEnvironmentVariable("GROQ_API_KEY")
              ?? Environment.GetEnvironmentVariable("GROQ__APIKEY")
              ?? Environment.GetEnvironmentVariable("Groq__ApiKey")
              ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userMessage) || string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            const string systemPrompt = @"Você é um assistente financeiro especialista em interpretar comandos, despesas e textos de OCR de comprovantes em português brasileiro informal.
Sua tarefa é analisar a mensagem ou texto de OCR do usuário e retornar EXCLUSIVAMENTE um JSON com os seguintes campos:
- intent: 'ADD_EXPENSE' (para compras/gastos, comprovantes de Pix/cartão, dumps de tela OCR de Nubank/Apple Pay), 'RECEIVE_PIX', 'DEPOSIT_CAIXINHA', 'PAY_INVOICE', 'INVEST_SURPLUS', 'SYNC_INVOICE', 'UPDATE_CAIXINHA', 'SIMULATE', 'STATUS', 'UNDO', ou 'UNKNOWN'.
- description: Título limpo e capitalizado do gasto, estabelecimento comercial ou recebedor/beneficiário do Pix (ex: para comprovante de Pix com 'Destino Nome Gustavo Antonio A Rodrigues', retorne 'Gustavo Antonio A Rodrigues'; para 'Posto Ipiranga', retorne 'Posto Ipiranga').
- amount: Valor numérico em reais (ex: 25.00, 35.00, 150.50, 4500.0, 0.01). ATENÇÃO CRÍTICA: NUNCA confunda números de datas (ex: '09 NOV 2026') ou horários com o valor monetário da transação! O valor monetário sempre vem acompanhado de 'Valor', 'R$' ou 'Total'.
- installments: Quantidade de parcelas (inteiro, padrão 1 se for à vista).
- category: Escolha exatamente uma entre: 'Alimentação', 'Transporte', 'Lazer', 'Compras', 'Assinaturas', 'Saúde', 'Entrada', 'Outros'.
- paymentMethod: 'PIX_INFLOW' se for recebimento/reembolso; 'PIX_CAIXINHA' se for transferência/comprovante Pix ou menção a pix/caixinha; 'PIX_ACCOUNT' se o usuário indicar explicitamente débito na conta; caso contrário 'CARD'.
- transactionDate: Se o usuário mencionar 'ontem' ou uma data específica no comprovante, retorne em formato 'YYYY-MM-DD', senão null.";

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

            if (messageContent.Contains("```"))
            {
                var fenceMatch = System.Text.RegularExpressions.Regex.Match(messageContent, @"```(?:json)?\s*([\s\S]*?)\s*```");
                if (fenceMatch.Success)
                {
                    messageContent = fenceMatch.Groups[1].Value.Trim();
                }
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
