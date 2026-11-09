using System.Net.Http.Headers;
using FinanceTracker.Domain.Http.Groq;
using Microsoft.Extensions.Options;

namespace FinanceTracker.Infrastructure.Http.Groq;

internal class GroqAuthHandler : DelegatingHandler
{
    private readonly GroqOptions _options;

    public GroqAuthHandler(IOptions<GroqOptions> options)
    {
        _options = options.Value;
    }

    private string GetApiKey() =>
        !string.IsNullOrWhiteSpace(_options.ApiKey)
            ? _options.ApiKey
            : Environment.GetEnvironmentVariable("GROQ_API_KEY")
              ?? Environment.GetEnvironmentVariable("GROQ__APIKEY")
              ?? Environment.GetEnvironmentVariable("Groq__ApiKey")
              ?? string.Empty;

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        return base.Send(request, cancellationToken);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
