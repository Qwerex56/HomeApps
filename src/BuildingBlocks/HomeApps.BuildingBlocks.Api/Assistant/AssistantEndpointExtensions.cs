using Microsoft.AspNetCore.Builder;

namespace HomeApps.BuildingBlocks.Api.Assistant;

public static class AssistantEndpointExtensions
{
    /// <summary>An assistant acting for the person may call this endpoint even where writes are denied by default.</summary>
    public static TBuilder AllowAssistant<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AssistantAccessMetadata(AssistantAccess.Allow));

    /// <summary>Never by an assistant, whatever the method and the service default.</summary>
    public static TBuilder DenyAssistant<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(new AssistantAccessMetadata(AssistantAccess.Deny));
}
