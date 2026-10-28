using Households.Domain.Households;
using Households.Domain.Invites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Households.Infrastructure.Persistence.Configurations;

/// <summary>
/// One row per household. Every change to the aggregate (memberships too) touches <c>UpdatedAt</c>, so the Postgres
/// <c>xmin</c> row version guards the whole aggregate: two joins racing for the last place (D10) cannot both win.
/// </summary>
internal sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        builder.ToTable("Households");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(h => h.DomainEvents);
        builder.Ignore(h => h.Owner);

        builder.Property(h => h.Name).HasConversion(Converters.HouseholdName).HasMaxLength(HouseholdName.MaxLength);
        builder.HasIndex(h => h.Name);
        builder.Property(h => h.Description).HasConversion(Converters.Description).HasMaxLength(HouseholdDescription.MaxLength);
        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.HasIndex(h => h.CreatedAt);

        builder.Property(h => h.GuestCode).HasConversion(Converters.OptionalCode).HasMaxLength(InviteCode.Length);
        builder.HasIndex(h => h.GuestCode).IsUnique();

        builder.HasMany(h => h.Members).WithOne().HasForeignKey(m => m.HouseholdId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(h => h.Members).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
    }
}

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");
        builder.HasKey(m => new { m.HouseholdId, m.UserId });
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.Property(m => m.Nickname).HasConversion(Converters.Nickname).HasMaxLength(Nickname.MaxLength);

        // "My households" (H2) and the account events (S3, S4) look memberships up by person.
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => m.InvitedBy);
        builder.HasIndex(m => m.AccessUntil).HasFilter("\"AccessUntil\" IS NOT NULL");

        // The invariant "exactly one Owner" also holds in the database, as a deferred exclusion constraint added by
        // the OneOwnerDeferred migration (EF cannot model it). A unique index is checked row by row, so a handover
        // failed whenever EF wrote the new Owner before the old one stepped down.
    }
}
