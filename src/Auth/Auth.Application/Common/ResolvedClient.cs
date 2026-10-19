using Auth.Application.Abstractions;
using Auth.Domain.Common;
using Auth.Domain.Identities;
using FluentResults;

namespace Auth.Application.Common;

/// <summary>A registered client and what the session stores about it.</summary>
internal sealed record ResolvedClient(RegisteredClient Registered, ClientInfo Info);

internal static class ClientRegistryExtensions
{
    /// <summary>Unknown clients get <c>client.unknown</c> before anything else happens.</summary>
    public static Result<ResolvedClient> Resolve(this IClientRegistry clients, string clientId, string? userAgent)
    {
        var registered = clients.Find(clientId);
        if (registered is null)
        {
            return Result.Fail<ResolvedClient>(CommonErrors.ClientInvalid);
        }

        var info = ClientInfo.Create(registered.ClientId, userAgent);
        return info.IsFailed ? info.ToResult<ResolvedClient>() : new ResolvedClient(registered, info.Value);
    }
}
