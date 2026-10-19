using Auth.Domain.Common;
using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>Which registered client asked for tokens (auth.md 3.1) and from what user agent. Shown in the session list.</summary>
public sealed record ClientInfo
{
    public const int ClientIdMaxLength = 64;
    public const int UserAgentMaxLength = 512;

    private ClientInfo(string clientId, string? userAgent)
    {
        ClientId = clientId;
        UserAgent = userAgent;
    }

    public string ClientId { get; }

    public string? UserAgent { get; }

    public static Result<ClientInfo> Create(string? clientId, string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > ClientIdMaxLength)
        {
            return Result.Fail<ClientInfo>(CommonErrors.ClientInvalid);
        }

        var agent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        if (agent is { Length: > UserAgentMaxLength })
        {
            agent = agent[..UserAgentMaxLength];
        }

        return new ClientInfo(clientId, agent);
    }
}
