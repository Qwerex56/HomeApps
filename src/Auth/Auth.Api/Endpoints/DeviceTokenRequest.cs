namespace Auth.Api.Endpoints;

internal sealed record DeviceTokenRequest(Guid DeviceId, string Secret);
