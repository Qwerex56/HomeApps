using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>
    /// Only for <see cref="ClientType.Service"/>, required there, at least <see cref="ClientRegistry.MinSecretLength"/>
    /// characters. A secret, so it comes from <c>.env</c> or a Docker secret, never from a committed file.
    /// </summary>
    public string? Secret { get; set; }
}

internal sealed class ClientRegistry : IClientRegistry
{
    public const string DefaultAudience = "homeapps";
    public const int MinSecretLength = 32;

    private readonly Dictionary<string, RegisteredClient> _clients;
    private readonly Dictionary<string, byte[]> _secretHashes = new(StringComparer.Ordinal);

    public ClientRegistry(IOptions<ClientsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _clients = options.Value.Clients.ToDictionary(
            c => c.ClientId,
            c => new RegisteredClient(
                c.ClientId, c.Name, c.Type, [.. c.RedirectUris], c.Audiences.Count == 0 ? [DefaultAudience] : [.. c.Audiences]),
            StringComparer.Ordinal);

        foreach (var client in options.Value.Clients.Where(c => c.Type == ClientType.Service))
        {
            if (client.Secret is null || client.Secret.Length < MinSecretLength)
            {
                throw new InvalidOperationException(
                    $"Auth:Clients: the service client '{client.ClientId}' needs a Secret of at least {MinSecretLength} characters.");
            }

            _secretHashes[client.ClientId] = Hash(client.Secret);
        }
    }

    public RegisteredClient? Find(string clientId) => _clients.GetValueOrDefault(clientId);

    public RegisteredClient? AuthenticateService(string clientId, string secret)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(secret)
            || !_secretHashes.TryGetValue(clientId, out var expected))
        {
            return null;
        }

        return CryptographicOperations.FixedTimeEquals(expected, Hash(secret)) ? _clients[clientId] : null;
    }

    private static byte[] Hash(string secret) => SHA256.HashData(Encoding.UTF8.GetBytes(secret));
}
