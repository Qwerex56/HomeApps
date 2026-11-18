namespace HomeApps.Contracts.Devices;

/// <summary>
/// jarvis-satellites.md 3a (S13): a household satellite paired and has its own identity in Auth. Households makes it a
/// member with the role Member, shown as <paramref name="Name"/>; it acts for the household, not for a person.
/// </summary>
public sealed record HouseholdDevicePaired(Guid DeviceId, Guid HouseholdId, string Name) : IIntegrationEvent;
