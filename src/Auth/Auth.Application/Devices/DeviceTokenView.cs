namespace Auth.Application.Devices;

public sealed record DeviceTokenView(string AccessToken, DateTimeOffset ExpiresAt);
