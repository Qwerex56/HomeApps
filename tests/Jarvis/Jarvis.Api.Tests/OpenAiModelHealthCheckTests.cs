using Jarvis.Infrastructure.Brain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jarvis.Api.Tests;

public sealed class OpenAiModelHealthCheckTests
{
    [Fact]
    public async Task An_unreachable_model_is_logged_as_critical_at_start_and_start_goes_on()
    {
        var log = new RecordingLogger<OpenAiModelHealthCheck>();
        var check = new OpenAiModelHealthCheck(
            new HttpClient(new ThrowingHandler()), Options.Create(new OpenAiChatOptions { BaseUrl = "https://models.test/v1", Model = "m" }), log);

        await check.StartAsync(CancellationToken.None);

        var critical = Assert.Single(log.Entries, e => e.Level == LogLevel.Critical);
        Assert.Contains("JARVIS model unavailable or lacks tool calling support", critical.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_malformed_URL_is_logged_as_critical_and_does_not_stop_start()
    {
        var log = new RecordingLogger<OpenAiModelHealthCheck>();
        var check = new OpenAiModelHealthCheck(
            new HttpClient(new FakeModel()), Options.Create(new OpenAiChatOptions { BaseUrl = "not a url", Model = "m" }), log);

        await check.StartAsync(CancellationToken.None);

        Assert.Contains(log.Entries, e => e.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task A_reachable_model_logs_nothing_critical()
    {
        var log = new RecordingLogger<OpenAiModelHealthCheck>();
        var check = new OpenAiModelHealthCheck(
            new HttpClient(new FakeModel()), Options.Create(new OpenAiChatOptions { BaseUrl = "https://models.test/v1", Model = "m" }), log);

        await check.StartAsync(CancellationToken.None);

        Assert.DoesNotContain(log.Entries, e => e.Level >= LogLevel.Error);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("connection refused");
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
