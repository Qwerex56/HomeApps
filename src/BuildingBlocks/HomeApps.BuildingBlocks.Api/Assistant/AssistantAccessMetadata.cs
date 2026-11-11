namespace HomeApps.BuildingBlocks.Api.Assistant;

/// <summary>Endpoint metadata read by <see cref="AssistantGuardMiddleware"/>; the last one added wins.</summary>
public sealed record AssistantAccessMetadata(AssistantAccess Access);
