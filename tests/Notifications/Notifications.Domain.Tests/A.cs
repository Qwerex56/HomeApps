using HomeApps.BuildingBlocks.Domain.Errors;
using Notifications.Domain.Recipients;

namespace Notifications.Domain.Tests;

internal static class A
{
    public static readonly DateTimeOffset Now = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A valid subscription as a browser gives it; each call a new endpoint.</summary>
    public static PushTarget Target(string? endpoint = null) =>
        PushTarget.Create(endpoint ?? $"https://push.example.com/{Guid.NewGuid():N}", Key(65), Key(16)).Value;

    public static string Key(int bytes) =>
        Convert.ToBase64String(Enumerable.Range(1, bytes).Select(i => (byte)i).ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static Recipient RecipientWithDevice()
    {
        var recipient = Recipient.Create(Guid.NewGuid());
        recipient.RegisterDevice(Target(), "Chrome", Now);
        return recipient;
    }

    public static string Code(this FluentResults.ResultBase result) => (string)result.Errors[0].Metadata[AppError.CodeKey];
}
