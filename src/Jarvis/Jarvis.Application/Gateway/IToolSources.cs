namespace Jarvis.Application.Gateway;

/// <summary>Every MCP server the gateway forwards to, with prefixed sources before the one without a prefix.</summary>
public interface IToolSources
{
    Task<IReadOnlyList<ToolSource>> ListAsync(CancellationToken cancellationToken);
}
