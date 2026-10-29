using Households.Domain.Households;
using Households.Domain.Invites;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Households.Infrastructure.Persistence.Configurations;

/// <summary>Value objects stored as one column. Reading back goes through the domain factory, so bad data fails loudly.</summary>
internal static class Converters
{
    public static readonly ValueConverter<HouseholdName, string> HouseholdName =
        new(n => n.Value, v => Domain.Households.HouseholdName.Create(v).Value);

    /// <summary>For nullable columns; EF never passes null to a converter, it stores null as is.</summary>
    public static readonly ValueConverter<HouseholdDescription?, string> Description =
        new(d => d!.Value, v => HouseholdDescription.Create(v).Value!);

    public static readonly ValueConverter<Nickname?, string> Nickname =
        new(n => n!.Value, v => Domain.Households.Nickname.Create(v).Value!);

    public static readonly ValueConverter<InviteCode, string> Code = new(c => c.Value, v => InviteCode.Parse(v).Value);

    public static readonly ValueConverter<InviteCode?, string> OptionalCode = new(c => c!.Value, v => InviteCode.Parse(v).Value);

    /// <summary>Enums are stored as their names: readable in the database and safe if members are reordered.</summary>
    public const int EnumLength = 32;
}
