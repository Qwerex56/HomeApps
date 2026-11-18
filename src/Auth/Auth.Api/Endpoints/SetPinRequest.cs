namespace Auth.Api.Endpoints;

internal sealed record SetPinRequest(string Pin, string? CurrentPassword);
