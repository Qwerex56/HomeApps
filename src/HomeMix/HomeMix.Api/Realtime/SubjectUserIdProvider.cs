using Microsoft.AspNetCore.SignalR;

namespace HomeMix.Api.Realtime;

/// <summary>The hub's user is the token's <c>sub</c> (inbound claims are not mapped).</summary>
internal sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection?.User.FindFirst("sub")?.Value;
}
