namespace Auth.Application.Abstractions;

/// <summary>The clients allowed to ask for tokens (auth.md 3.1), from configuration <c>Auth:Clients</c>.</summary>
public interface IClientRegistry
{
    RegisteredClient? Find(string clientId);

    /// <summary>
    /// A <see cref="ClientType.Service"/> client proving who it is with its secret (compared in constant time);
    /// <c>null</c> for an unknown client, another type or a wrong secret.
    /// </summary>
    RegisteredClient? AuthenticateService(string clientId, string secret);
}

/// <summary>Browser clients get the refresh token in an HttpOnly cookie, native ones in the response body.</summary>
public enum ClientType
{
    Browser = 0,
    Native = 1,

    /// <summary>
    /// A HomeApps service acting for people (JARVIS): never signs anyone in, only asks for delegated tokens with its
    /// secret on the internal network (jarvis-v1.md, section 3).
    /// </summary>
    Service = 2,
}

public sealed record RegisteredClient(
    string ClientId,
    string Name,
    ClientType Type,
    IReadOnlyList<Uri> RedirectUris,
    IReadOnlyList<string> Audiences);
