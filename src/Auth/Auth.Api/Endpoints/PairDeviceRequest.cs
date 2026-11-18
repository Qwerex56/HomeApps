namespace Auth.Api.Endpoints;

internal sealed record PairDeviceRequest(string ClientId, string ClientSecret, Guid DeviceId, Guid HouseholdId, string Name);
