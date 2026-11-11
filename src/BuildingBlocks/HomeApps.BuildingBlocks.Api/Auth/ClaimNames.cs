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

    /// <summary>
    /// RFC 8693: who acts for the person, e.g. <c>{"sub":"jarvis"}</c>. Only tokens Auth delegates to an assistant
    /// carry it; services read data for it like for the person but Auth, Accounts and Households refuse its changes.
    /// </summary>
    public const string Actor = "act";

    /// <summary>How the token was obtained; <c>delegated</c> for an assistant acting for the person.</summary>
    public const string AuthenticationMethods = "amr";
}
