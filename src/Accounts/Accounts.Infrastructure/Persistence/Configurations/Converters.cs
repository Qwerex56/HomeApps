using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Accounts.Infrastructure.Persistence.Configurations;

/// <summary>Value objects stored as one column. Reading back goes through the domain factory, so bad data fails loudly.</summary>
internal static class Converters
{
    public static readonly ValueConverter<Email, string> Email = new(e => e.Value, v => Domain.Common.Email.Create(v).Value);

    public static readonly ValueConverter<DisplayName, string> DisplayName =
        new(n => n.Value, v => Domain.Users.DisplayName.Create(v).Value);

    /// <summary>For nullable columns; EF never passes null to a converter, it stores null as is.</summary>
    public static readonly ValueConverter<PersonName?, string> PersonName =
        new(n => n!.Value, v => Domain.Users.PersonName.Create(v, "name").Value!);

    public static readonly ValueConverter<AvatarUrl?, string> AvatarUrl =
        new(a => a!.Value.ToString(), v => Domain.Users.AvatarUrl.Create(v).Value!);

    public static readonly ValueConverter<Locale, string> Locale = new(l => l.Value, v => Domain.Users.Locale.Create(v).Value);

    public static readonly ValueConverter<TimeZoneId, string> TimeZone =
        new(z => z.Value, v => TimeZoneId.Create(v).Value);

    /// <summary>Enums are stored as their names: readable in the database and safe if members are reordered.</summary>
    public const int EnumLength = 32;

    /// <summary>A BCP 47 tag or an IANA zone name.</summary>
    public const int CodeLength = 64;
}
