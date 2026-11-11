using HomeApps.BuildingBlocks.Application.Messaging;

namespace Auth.Application.Delegation.DelegateToAssistant;

/// <summary>
/// jarvis-v1.md, section 3 (JV3): a service client (JARVIS) asks for a short token to act for one person, e.g. the
/// author of a message that mentions it. Only on the internal network (Traefik does not route <c>/internal</c>), and
/// only with the client's secret. <paramref name="Channel"/> and <paramref name="Reference"/> say why, for the log.
/// </summary>
public sealed record DelegateToAssistantCommand(
    string ClientId,
    string ClientSecret,
    Guid UserId,
    string Channel,
    string? Reference) : ICommand<DelegatedTokenView>;
