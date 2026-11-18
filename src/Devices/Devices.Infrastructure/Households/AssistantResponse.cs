namespace Devices.Infrastructure.Households;

/// <summary>The part of Households' internal <c>GET /internal/households/{id}/assistant</c> Devices reads (D9).</summary>
internal sealed record AssistantResponse(int EventDays, int LocationEventDays);
