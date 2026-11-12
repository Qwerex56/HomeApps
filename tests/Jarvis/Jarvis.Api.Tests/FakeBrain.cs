using System.Runtime.CompilerServices;
using HomeApps.BuildingBlocks.Testing;
using Jarvis.Application.Abstractions;
using Jarvis.Infrastructure.Gateway;
using ModelContextProtocol.Client;

namespace Jarvis.Api.Tests;

/// <summary>
/// A brain without a model: it connects to the gateway with the run's token and <c>X-Jarvis-Run</c> header, like Claude
/// Code does, runs the test's <see cref="Script"/> and answers what it returns.
/// </summary>
internal sealed class FakeBrain : IAssistantBrain
{
    public Func<McpClient, Task<string>> Script { get; set; } = _ => Task.FromResult("Gotowe.");

    public decimal Cost { get; set; } = 0.01m;

    public bool IsReady { get; set; } = true;

    public string? LastPrompt { get; private set; }

    public Func<string, HttpClient> Gateway { get; set; } = _ => throw new InvalidOperationException("Not started.");

    public async IAsyncEnumerable<BrainEvent> RunAsync(BrainRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        LastPrompt = request.Prompt;
        var http = Gateway(request.GatewayToken);
        http.DefaultRequestHeaders.Add(RunHeader.Name, request.RunId.ToString());
        await using var mcp = await McpTestClient.ConnectAsync(http, "jarvis/mcp", cancellationToken);
        var text = await Script(mcp);
        yield return new BrainText(text);
        yield return new BrainFinished(text, "fake", Cost);
    }
}
