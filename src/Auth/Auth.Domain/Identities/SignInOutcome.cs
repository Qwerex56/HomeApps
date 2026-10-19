namespace Auth.Domain.Identities;

/// <summary>What a correct sign-in leads to.</summary>
public abstract record SignInOutcome
{
    private SignInOutcome() { }

    /// <summary>A new session started.</summary>
    public sealed record SignedIn(IssuedRefreshToken Token) : SignInOutcome;

    /// <summary>
    /// R16: the user disabled the account themselves. No session; Application issues a reactivation ticket instead
    /// (403 <c>account.disabled_by_user</c>), remembering the method for <see cref="Identity.CompleteReactivation"/>.
    /// </summary>
    public sealed record ReactivationRequired(Guid IdentityId, AuthMethod Method) : SignInOutcome;
}
