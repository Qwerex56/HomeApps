namespace HomeApps.BuildingBlocks.Api.Auth;

/// <summary>Claim names in HomeApps access tokens (inbound claim mapping is off, so these are the raw JWT names).</summary>
public static class ClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string TokenId = "jti";

    /// <summary>The registered client the token was issued to (auth.md 3.1).</summary>
    public const string ClientId = "client_id";

    /// <summary>When the user actually signed in, in Unix seconds; a refresh keeps it (R8).</summary>
    public const string AuthTime = "auth_time";
}
