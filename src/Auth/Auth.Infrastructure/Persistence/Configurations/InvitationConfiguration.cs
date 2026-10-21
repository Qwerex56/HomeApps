using Auth.Domain.Common;
using Auth.Domain.Invitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Ignore(i => i.DomainEvents);

        builder.Property(i => i.Email).HasConversion(Converters.OptionalEmail).HasMaxLength(Email.MaxLength);
        builder.HasIndex(i => i.Email);
        builder.Property(i => i.TokenHash).HasMaxLength(Converters.TokenHashLength);
        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.Property(i => i.Code).HasConversion(Converters.InvitationCode).HasMaxLength(7);
        builder.HasIndex(i => i.Code).IsUnique();
        builder.Property(i => i.Kind).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.Property(i => i.InvitedByEmail).HasConversion(Converters.OptionalEmail).HasMaxLength(Email.MaxLength);
        builder.HasIndex(i => i.InvitedByUserId);
    }
}
