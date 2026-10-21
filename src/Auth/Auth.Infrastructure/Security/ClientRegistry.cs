using System.ComponentModel.DataAnnotations;
using Auth.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Auth.Infrastructure.Security;

/// <summary>Section <c>Auth</c>, key <c>Clients</c>: the apps allowed to ask for tokens (auth.md 3.1).</summary>
public sealed class ClientsOptions
{
    public const string SectionName = "Auth";

    public IList<ClientOptions> Clients { get; } = [];
}

public sealed class ClientOptions
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public ClientType Type { get; set; }

    public IList<Uri> RedirectUris { get; } = [];

    /// <summary>The <c>aud</c> values of this client's access tokens; empty means the default <c>homeapps</c>.</summary>
    public IList<string> Audiences { get; } = [];
}

internal sealed class ClientRegistry(IOptions<ClientsOptions> options) : IClientRegistry
{
    public const string DefaultAudience = "homeapps";

    private readonly Dictionary<string, RegisteredClient> _clients = options.Value.Clients.ToDictionary(
        c => c.ClientId,
        c => new RegisteredClient(
            c.ClientId, c.Name, c.Type, [.. c.RedirectUris], c.Audiences.Count == 0 ? [DefaultAudience] : [.. c.Audiences]),
        StringComparer.Ordinal);

    public RegisteredClient? Find(string clientId) => _clients.GetValueOrDefault(clientId);
}
