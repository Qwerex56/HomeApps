namespace Auth.Api.Endpoints;

internal sealed record IdentifySpeakerRequest(string ClientId, string ClientSecret, Guid DeviceId, IReadOnlyList<Guid> Candidates, string Pin);
