namespace HomeApps.BuildingBlocks.Api.Assistant;

/// <summary>How a service treats calls made by an assistant on a person's behalf (jarvis-v1.md, JV13).</summary>
public sealed class AssistantGuardOptions
{
    /// <summary>
    /// True (the default, for Auth, Accounts and Households): an assistant may only read; every request that is not
    /// GET, HEAD or OPTIONS answers 403 <c>assistant.not_allowed</c> unless the endpoint says
    /// <see cref="AssistantEndpointExtensions.AllowAssistant{TBuilder}"/>. Plugins turn it off: an assistant changes
    /// plugin data with the person's own rights.
    /// </summary>
    public bool DenyWritesByDefault { get; set; } = true;
}
