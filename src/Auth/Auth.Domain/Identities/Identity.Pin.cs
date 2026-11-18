using Auth.Domain.Common;
using FluentResults;

namespace Auth.Domain.Identities;

/// <summary>
/// The spoken PIN (jarvis-satellites.md S4): a household satellite hears "tu Adrian, PIN 1234" and JARVIS answers that
/// turn as this person. Set with the same proof as a password change (R8); stored only as a password hash.
/// </summary>
public sealed partial class Identity
{
    public const int PinMinLength = 4;
    public const int PinMaxLength = 8;

    public string? PinHash { get; private set; }

    public bool HasPin => PinHash is not null;

    public Result SetPin(string? currentPassword, DateTimeOffset authTime, string pin, IPasswordHasher hasher, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        ArgumentNullException.ThrowIfNull(time);

        var now = time.GetUtcNow();
        var proof = ProveIdentity(currentPassword, authTime, hasher, now);
        if (proof.IsFailed)
        {
            return proof;
        }

        if (!IsValidPin(pin))
        {
            return Result.Fail(IdentityErrors.PinInvalid);
        }

        PinHash = hasher.Hash(pin);
        Touch(now);
        return Result.Ok();
    }

    public void ClearPin(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        PinHash = null;
        Touch(time.GetUtcNow());
    }

    /// <summary>Only an active account with a PIN answers to one.</summary>
    public bool AcceptsPin(string pin, IPasswordHasher hasher)
    {
        ArgumentNullException.ThrowIfNull(hasher);
        return Status == AccountStatus.Active && PinHash is not null && IsValidPin(pin) && hasher.Verify(PinHash, pin);
    }

    public static bool IsValidPin(string? pin) =>
        pin is { Length: >= PinMinLength and <= PinMaxLength } && pin.All(char.IsAsciiDigit);
}
