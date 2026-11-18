namespace Devices.Application.Abstractions;

/// <summary>
/// One message of a household's stream: <paramref name="Type"/> is <c>state</c>, <c>event</c>, <c>status</c> or <c>online</c>.
/// <paramref name="MembersOnly"/> messages (events, D9) do not reach guests.
/// </summary>
public sealed record DeviceStreamMessage(Guid HouseholdId, string Type, object Data, bool MembersOnly = false);
