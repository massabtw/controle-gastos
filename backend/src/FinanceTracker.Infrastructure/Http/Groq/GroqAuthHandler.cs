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

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        return base.Send(request, cancellationToken);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
