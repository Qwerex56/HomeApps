using System.Net.Http.Headers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// At start: asks the model API for its models and logs a critical error when it does not answer, so a wrong
/// URL or key shows up in the logs at once instead of at the first question. Never stops the service from starting.
/// </summary>
internal sealed partial class OpenAiModelHealthCheck(HttpClient http, IOptions<OpenAiChatOptions> options, ILogger<OpenAiModelHealthCheck> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.Model))
        {
            LogNotConfigured();
            return;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(settings.Timeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, settings.BaseUrl.TrimEnd('/') + "/models");
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            }

            using var response = await http.SendAsync(request, timeout.Token);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (exception is HttpRequestException or UriFormatException or InvalidOperationException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogUnavailable(exception);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Warning, Message = "JARVIS has no model: Jarvis:OpenAi:BaseUrl or Model is empty, he will say he is switched off.")]
    private partial void LogNotConfigured();

    [LoggerMessage(Level = LogLevel.Critical, Message = "JARVIS model unavailable or lacks tool calling support")]
    private partial void LogUnavailable(Exception exception);
}
