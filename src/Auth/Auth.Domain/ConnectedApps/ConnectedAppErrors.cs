using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Domain.ConnectedApps;

/// <summary>
/// OAuth errors. The part after <see cref="Prefix"/> is the OAuth <c>error</c> value (RFC 6749 5.2, RFC 7591 3.2.2),
/// which the Api answers in OAuth's own shape on the OAuth endpoints.
/// </summary>
public static class ConnectedAppErrors
{
    public const string Prefix = "oauth.";

    public static Error InvalidRequest(string message) => new AppError(Prefix + "invalid_request", message, ErrorKind.Validation);

    public static Error InvalidClient => new AppError(Prefix + "invalid_client", "The app is not registered.", ErrorKind.Unauthorized);

    public static Error InvalidGrant => new AppError(Prefix + "invalid_grant",
        "The code or refresh token is not valid, has expired or belongs to another app.", ErrorKind.Validation);

    public static Error CodeReused => new AppError(Prefix + "invalid_grant", "The code was already used.", ErrorKind.Validation);

    public static Error UnsupportedGrantType => new AppError(Prefix + "unsupported_grant_type",
        "Only authorization_code and refresh_token are supported.", ErrorKind.Validation);

    public static Error InvalidTarget => new AppError(Prefix + "invalid_target",
        "The resource is not one this server issues tokens for.", ErrorKind.Validation);

    public static Error InvalidRedirectUri(string message) => new AppError(Prefix + "invalid_redirect_uri", message, ErrorKind.Validation);

    public static Error InvalidClientMetadata(string message) => new AppError(Prefix + "invalid_client_metadata", message, ErrorKind.Validation);

    public static Error Disabled => new AppError(Prefix + "access_denied",
        "Connected apps are turned off on this server (Auth:ConnectedApps:Resources is empty).", ErrorKind.Forbidden);

    public static Error TooManyClients => new AppError(Prefix + "temporarily_unavailable",
        "Too many apps are registered right now. Try again later.", ErrorKind.Conflict);
}
