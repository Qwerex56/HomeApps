namespace Devices.Application.Abstractions;

/// <summary>
/// One message of a household's stream: <paramref name="Type"/> is <c>state</c>, <c>event</c>, <c>status</c>, <c>online</c> or <c>changed</c>.
/// <paramref name="MembersOnly"/> messages (events, D9; state of location devices, R3) do not reach guests.
/// </summary>
public sealed record DeviceStreamMessage(Guid HouseholdId, string Type, object Data, bool MembersOnly = false);

