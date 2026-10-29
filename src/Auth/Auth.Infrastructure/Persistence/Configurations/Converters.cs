using Auth.Domain.Common;
using Auth.Domain.Invitations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Auth.Infrastructure.Persistence.Configurations;

/// <summary>Value objects stored as one column. Reading back goes through the domain factory, so bad data fails loudly.</summary>
internal static class Converters
{
    public static readonly ValueConverter<Email, string> Email = new(e => e.Value, v => Domain.Common.Email.Create(v).Value);

    /// <summary>For nullable columns; EF never passes null to a converter, it stores null as is.</summary>
    public static readonly ValueConverter<Email?, string> OptionalEmail =
        new(e => e!.Value, v => Domain.Common.Email.Create(v).Value);

    public static readonly ValueConverter<InvitationCode, string> InvitationCode =
        new(c => c.Value, v => Domain.Invitations.InvitationCode.Create(v).Value);

    /// <summary>Hex SHA-256 of an opaque token.</summary>
    public const int TokenHashLength = 64;

    /// <summary>Enums are stored as their names: readable in the database and safe if members are reordered.</summary>
    public const int EnumLength = 32;
}
