using HomeMix.Application.Abstractions;
using HomeMix.Application.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HomeMix.Api.Realtime;

/// <summary>
/// <c>/homemix/hub</c> (docs/homemix.md, "RealTime"): the web app connects once with the person's token
/// (<c>?access_token=</c>) and hears about the add-ons it shows. A connection is in its person's group; it joins a
/// household's group only as one of its members (guests included), asked from Households with the same token.
/// Browsers only listen: everything they change goes through the API.
/// </summary>
[Authorize]
public sealed class HomeMixHub(IHouseholdRoles households, IHttpContextAccessor accessor) : Hub
{
    public const string Path = "/homemix/hub";

    /// <summary>The one client method: the event's name and its data.</summary>
    public const string ClientMethod = "event";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.User(UserId()));
        await base.OnConnectedAsync();
    }

    /// <summary>Starts the household's events for this connection; false without access.</summary>
    public async Task<bool> WatchHousehold(Guid householdId)
    {
        // The current user and the call to Households read the connection's request (its token), whatever the transport.
        accessor.HttpContext = Context.GetHttpContext();
        if (await households.GetRoleAsync(householdId, Context.ConnectionAborted) is null)
        {
            return false;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Household(householdId), Context.ConnectionAborted);
        return true;
    }

    public Task UnwatchHousehold(Guid householdId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.Household(householdId), Context.ConnectionAborted);

    private Guid UserId() =>
        Guid.TryParse(Context.UserIdentifier, out var id) ? id : throw new HubException("No user in the token.");
}
