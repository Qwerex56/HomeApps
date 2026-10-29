using Accounts.Domain.Common;
using FluentResults;

namespace Accounts.Domain.Users;

/// <summary>Why and by whom a disabled account was disabled. Gone again when the account is enabled.</summary>
public sealed record Disabling
{
    public const int ReasonMaxLength = 500;

    private Disabling(DateTimeOffset at, Guid byUserId, string? reason)
    {
        At = at;
        ByUserId = byUserId;
        Reason = reason;
    }

    public DateTimeOffset At { get; }

    /// <summary>Who disabled it; the account's own id when the user did it (R8). Hidden from the user (accounts.md 3.2, Q2).</summary>
    public Guid ByUserId { get; }

    public string? Reason { get; }

    internal static Result<Disabling> Create(DateTimeOffset at, Guid byUserId, string? reason)
    {
        var text = Text.OrNull(reason);
        if (text is { Length: > ReasonMaxLength } || (text is not null && Text.HasControlCharacters(text.Replace("\r\n", " ", StringComparison.Ordinal).Replace('\n', ' '))))
        {
            return Result.Fail<Disabling>(UserErrors.ReasonInvalid);
        }

        return new Disabling(at, byUserId, text);
    }
}
