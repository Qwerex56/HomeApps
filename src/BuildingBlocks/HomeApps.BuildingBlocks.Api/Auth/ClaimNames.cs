namespace HomeApps.BuildingBlocks.Api.Auth;

/// <summary>Claim names in HomeApps access tokens (inbound claim mapping is off, so these are the raw JWT names).</summary>
public static class ClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string TokenId = "jti";
}
