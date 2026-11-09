namespace Notifications.Application.Settings.GetSettings;

/// <param name="Source"><c>null</c>: the whole household.</param>
public sealed record MuteView(Guid HouseholdId, string? Source);
