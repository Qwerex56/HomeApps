using FluentValidation;

namespace Auth.Application.ConnectedApps.RegisterApp;

/// <summary>Only what this server supports: a public client (no secret) with the code flow and refresh tokens.</summary>
internal sealed class RegisterAppValidator : AbstractValidator<RegisterAppCommand>
{
    private static readonly string[] SupportedGrantTypes = ["authorization_code", "refresh_token"];

    public RegisterAppValidator()
    {
        RuleFor(c => c.TokenEndpointAuthMethod)
            .Must(m => m is null or "none")
            .WithMessage("Only public clients (token_endpoint_auth_method none) are supported.");
        RuleFor(c => c.GrantTypes)
            .Must(g => g is null || g.All(SupportedGrantTypes.Contains))
            .WithMessage("Only the authorization_code and refresh_token grants are supported.");
        RuleFor(c => c.ResponseTypes)
            .Must(r => r is null || r.All(t => t == "code"))
            .WithMessage("Only the code response type is supported.");
    }
}
