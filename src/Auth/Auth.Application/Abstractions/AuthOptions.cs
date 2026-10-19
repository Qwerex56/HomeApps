namespace Auth.Application.Abstractions;

/// <summary>Section <c>Auth</c>.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>False: sign-up only through an invitation (R15). Off by default, it is a home server.</summary>
    public bool RegistrationOpen { get; set; }

    public BootstrapOptions Bootstrap { get; set; } = new();
}

public sealed class BootstrapOptions
{
    /// <summary>A-30: the first Owner's email. Without it no bootstrap invitation is created.</summary>
    public string? Email { get; set; }
}
