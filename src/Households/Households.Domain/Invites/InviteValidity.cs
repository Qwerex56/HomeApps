namespace Households.Domain.Invites;

/// <summary>D3b: how long an invite works, Discord-style. No "never": only the guest link is permanent (D2b).</summary>
public enum InviteValidity
{
    ThirtyMinutes = 0,
    OneHour = 1,
    SixHours = 2,
    TwelveHours = 3,
    OneDay = 4,

    /// <summary>The default.</summary>
    SevenDays = 5,
}

public static class InviteValidityExtensions
{
    public static TimeSpan Duration(this InviteValidity validity) => validity switch
    {
        InviteValidity.ThirtyMinutes => TimeSpan.FromMinutes(30),
        InviteValidity.OneHour => TimeSpan.FromHours(1),
        InviteValidity.SixHours => TimeSpan.FromHours(6),
        InviteValidity.TwelveHours => TimeSpan.FromHours(12),
        InviteValidity.OneDay => TimeSpan.FromDays(1),
        InviteValidity.SevenDays => TimeSpan.FromDays(7),
        _ => throw new ArgumentOutOfRangeException(nameof(validity), validity, null),
    };
}
