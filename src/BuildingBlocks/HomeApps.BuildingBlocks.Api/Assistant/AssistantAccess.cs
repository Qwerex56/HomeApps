namespace HomeApps.BuildingBlocks.Api.Assistant;

/// <summary>
/// What an endpoint says about calls made by an assistant (JARVIS, or an app connected over OAuth) on a person's
/// behalf: a token that carries the <c>act</c> claim (RFC 8693). People's own tokens are never affected.
/// </summary>
public enum AssistantAccess
{
    /// <summary>The service default decides (<see cref="AssistantGuardOptions.DenyWritesByDefault"/>).</summary>
    Default = 0,

    /// <summary>An assistant may call it, e.g. the MCP endpoint of a core service, whose tools only read.</summary>
    Allow = 1,

    /// <summary>Never by an assistant, whatever the HTTP method, e.g. switching a plugin on in a household.</summary>
    Deny = 2,
}
