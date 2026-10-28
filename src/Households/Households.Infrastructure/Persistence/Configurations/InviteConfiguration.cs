using Households.Domain.Households;
using Households.Domain.Invites;
using Households.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Households.Infrastructure.Persistence.Configurations;

internal sealed class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.ToTable("Invites");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property<uint>("Version").IsRowVersion();

        builder.Property(i => i.Code).HasConversion(Converters.Code).HasMaxLength(InviteCode.Length);
        builder.HasIndex(i => i.Code).IsUnique();
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.HasIndex(i => new { i.HouseholdId, i.CreatedBy });
        builder.HasIndex(i => i.CreatedBy);

        builder.HasOne<Household>().WithMany().HasForeignKey(i => i.HouseholdId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class UserCopyConfiguration : IEntityTypeConfiguration<UserCopy>
{
    public void Configure(EntityTypeBuilder<UserCopy> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.DisplayName).HasMaxLength(UserCopy.DisplayNameMaxLength);
        builder.Property(u => u.AvatarUrl).HasMaxLength(UserCopy.AvatarUrlMaxLength);
    }
}
