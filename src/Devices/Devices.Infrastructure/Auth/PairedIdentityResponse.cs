namespace Devices.Infrastructure.Auth;

internal sealed record PairedIdentityResponse(Guid DeviceId, string Secret);
