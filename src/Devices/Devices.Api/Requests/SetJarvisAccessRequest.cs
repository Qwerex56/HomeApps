namespace Devices.Api.Requests;

public sealed record SetJarvisAccessRequest(bool MayControl, bool Asks);
