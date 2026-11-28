using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jarvis.Infrastructure.Brain;

/// <summary>
/// At start, and after an admin saves the settings: asks the model API for its models and logs a critical error when it
/// does not answer, so a wrong URL or key shows up in the logs at once instead of at the first question. Never stops
/// the service from starting. <see cref="CheckAsync"/> is also what the admin's "test connection" runs.
/// </summary>
public sealed partial class OpenAiModelHealthCheck(HttpClient http, IServiceScopeFactory scopes, ILogger<OpenAiModelHealthCheck> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var settings = await scope.ServiceProvider.GetRequiredService<ILlmConnection>().CurrentAsync(cancellationToken);
        await CheckAsync(settings, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<LlmCheckResult> CheckAsync(OpenAiChatOptions settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.Model))
        {
            LogNotConfigured();
            return new LlmCheckResult(false, "not configured");
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
            return new LlmCheckResult(true, null);
        }
        catch (Exception exception) when (exception is HttpRequestException or UriFormatException or InvalidOperationException
            || exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            LogUnavailable(exception);
            return new LlmCheckResult(false, exception switch
            {
                HttpRequestException { StatusCode: { } status } => $"HTTP {(int)status}",
                HttpRequestException => "unreachable",
                OperationCanceledException => "timeout",
                _ => "invalid URL",
            });
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "JARVIS has no model: BaseUrl or Model is empty (settings in the admin panel or Jarvis:OpenAi), he will say he is switched off.")]
    private partial void LogNotConfigured();

    [LoggerMessage(Level = LogLevel.Critical, Message = "JARVIS model unavailable or lacks tool calling support")]
    private partial void LogUnavailable(Exception exception);
}
