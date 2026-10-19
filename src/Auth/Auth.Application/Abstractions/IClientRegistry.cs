namespace Auth.Application.Abstractions;

/// <summary>The clients allowed to ask for tokens (auth.md 3.1), from configuration <c>Auth:Clients</c>.</summary>
public interface IClientRegistry
{
    RegisteredClient? Find(string clientId);
}

/// <summary>Browser clients get the refresh token in an HttpOnly cookie, native ones in the response body.</summary>
public enum ClientType
{
    Browser = 0,
    Native = 1,
}

public sealed record RegisteredClient(
    string ClientId,
    string Name,
    ClientType Type,
    IReadOnlyList<Uri> RedirectUris,
    IReadOnlyList<string> Audiences);
