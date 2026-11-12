using Jarvis.Application.Gateway;
using Jarvis.Infrastructure.Gateway;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jarvis.Api.Gateway;

/// <summary>
/// The JARVIS gateway (jarvis-v1.md 2.3, 10a): one MCP server in front of every source. Lists tools per person with
/// that person's token and nothing kept between people (HomeMix shows each person only their add-ons); names get the
/// source's prefix (<c>todo_add_task</c>), a second tool with a name already taken is left out and logged; calls and
/// resource reads go to the source that owns the name. A source that does not answer leaves out only its own tools.
/// </summary>
internal sealed partial class McpGateway(IToolSources sources, SourceConnector connector, Caller caller, ILogger<McpGateway> logger)
{
    public async ValueTask<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken)
    {
        var all = await sources.ListAsync(cancellationToken);
        var token = await caller.SourceTokenAsync(cancellationToken);
        var perSource = await Task.WhenAll(all.Select(source => ToolsOfAsync(source, token, cancellationToken)));

        var tools = new List<Tool>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (source, tool) in perSource.SelectMany(list => list))
        {
            var name = source.Expose(tool.Name);
            if (!names.Add(name))
            {
                LogDuplicate(name, source.Name);
                continue;
            }

            tools.Add(new Tool
            {
                Name = name,
                Title = tool.Title,
                Description = tool.Description,
                InputSchema = tool.InputSchema,
                OutputSchema = tool.OutputSchema,
                Annotations = tool.Annotations,
                Icons = tool.Icons,
                Meta = tool.Meta,
            });
        }

        return new ListToolsResult { Tools = tools };
    }

    public async ValueTask<CallToolResult> CallToolAsync(CallToolRequestParams request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        foreach (var source in await sources.ListAsync(cancellationToken))
        {
            if (source.Own(request.Name) is not { } own)
            {
                continue;
            }

            try
            {
                await using var client = await connector.ConnectAsync(source, await caller.SourceTokenAsync(cancellationToken), cancellationToken);
                return await client.CallToolAsync(
                    new CallToolRequestParams { Name = own, Arguments = request.Arguments, Meta = request.Meta }, cancellationToken);
            }
            catch (McpException exception)
            {
                return Failed(exception.Message);
            }
            catch (Exception exception) when (Unreachable(exception) && !cancellationToken.IsCancellationRequested)
            {
                LogUnreachable(source.Name, exception);
                return Failed($"{source.Name} did not answer; try again in a moment.");
            }
        }

        return Failed($"There is no tool named {request.Name}.");
    }

    public async ValueTask<ListResourcesResult> ListResourcesAsync(CancellationToken cancellationToken)
    {
        var all = await sources.ListAsync(cancellationToken);
        var token = await caller.SourceTokenAsync(cancellationToken);
        var perSource = await Task.WhenAll(all.Select(source => ResourcesOfAsync(source, token, cancellationToken)));
        return new ListResourcesResult { Resources = [.. perSource.SelectMany(list => list)] };
    }

    public async ValueTask<ReadResourceResult> ReadResourceAsync(ReadResourceRequestParams request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var colon = request.Uri.IndexOf(':', StringComparison.Ordinal);
        var name = colon > 0 ? request.Uri[..colon] : string.Empty;
        var source = (await sources.ListAsync(cancellationToken)).FirstOrDefault(s => s.Name == name)
            ?? throw new McpException($"There is no resource {request.Uri}.");

        await using var client = await connector.ConnectAsync(source, await caller.SourceTokenAsync(cancellationToken), cancellationToken);
        var read = await client.ReadResourceAsync(new ReadResourceRequestParams { Uri = request.Uri[(colon + 1)..], Meta = request.Meta }, cancellationToken);
        foreach (var content in read.Contents)
        {
            content.Uri = source.ExposeUri(content.Uri);
        }

        return read;
    }

    private async Task<IReadOnlyList<(ToolSource Source, Tool Tool)>> ToolsOfAsync(ToolSource source, string token, CancellationToken cancellationToken)
    {
        try
        {
            await using var client = await connector.ConnectAsync(source, token, cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            return [.. tools.Select(t => (source, t.ProtocolTool))];
        }
        catch (Exception exception) when ((Unreachable(exception) || exception is McpException) && !cancellationToken.IsCancellationRequested)
        {
            LogUnreachable(source.Name, exception);
            return [];
        }
    }

    private async Task<IReadOnlyList<Resource>> ResourcesOfAsync(ToolSource source, string token, CancellationToken cancellationToken)
    {
        try
        {
            await using var client = await connector.ConnectAsync(source, token, cancellationToken);
            if (client.ServerCapabilities.Resources is null)
            {
                return [];
            }

            var resources = await client.ListResourcesAsync(cancellationToken: cancellationToken);
            return [.. resources.Select(r => new Resource
            {
                Uri = source.ExposeUri(r.ProtocolResource.Uri),
                Name = r.ProtocolResource.Name,
                Title = r.ProtocolResource.Title,
                Description = r.ProtocolResource.Description,
                MimeType = r.ProtocolResource.MimeType,
                Size = r.ProtocolResource.Size,
                Annotations = r.ProtocolResource.Annotations,
                Meta = r.ProtocolResource.Meta,
            })];
        }
        catch (Exception exception) when ((Unreachable(exception) || exception is McpException) && !cancellationToken.IsCancellationRequested)
        {
            LogUnreachable(source.Name, exception);
            return [];
        }
    }

    private static bool Unreachable(Exception exception) =>
        exception is HttpRequestException or TimeoutException or TaskCanceledException;

    private static CallToolResult Failed(string text) =>
        new() { Content = [new TextContentBlock { Text = text }], IsError = true };

    [LoggerMessage(Level = LogLevel.Warning, Message = "The MCP source {Source} did not answer; its tools are left out of this request.")]
    private partial void LogUnreachable(string source, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tool {Tool} from {Source} is left out: another source already has that name.")]
    private partial void LogDuplicate(string tool, string source);
}
