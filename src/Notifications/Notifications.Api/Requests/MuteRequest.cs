namespace Notifications.Api.Requests;

/// <param name="Source">A plugin id such as <c>chat</c>; <c>null</c> mutes the whole household.</param>
internal sealed record MuteRequest(Guid HouseholdId, string? Source);
