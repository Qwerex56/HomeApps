using Auth.Domain.Common;
using Auth.Domain.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

internal sealed class IdentityConfiguration : IEntityTypeConfiguration<Identity>
{
    public void Configure(EntityTypeBuilder<Identity> builder)
    {
        builder.ToTable("Identities");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(i => i.DomainEvents);

        builder.Property(i => i.Email).HasConversion(Converters.Email).HasMaxLength(Email.MaxLength);
        builder.HasIndex(i => i.Email).IsUnique();
        builder.Property(i => i.PasswordHash).HasMaxLength(256);
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.Property(i => i.Source).HasConversion<string>().HasMaxLength(Converters.EnumLength);

        builder.ComplexProperty(i => i.Lockout, lockout =>
        {
            lockout.Property(l => l.FailedAttempts).HasColumnName("FailedSignInAttempts");
            lockout.Property(l => l.LockedUntil).HasColumnName("LockedUntil");
        });

        builder.ComplexProperty(i => i.EmailToken, token =>
        {
            token.Property(t => t.TokenHash).HasColumnName("EmailTokenHash").HasMaxLength(Converters.TokenHashLength);
            token.Property(t => t.Purpose).HasColumnName("EmailTokenPurpose").HasConversion<string>().HasMaxLength(Converters.EnumLength);
            token.Property(t => t.ExpiresAt).HasColumnName("EmailTokenExpiresAt");
            token.Property(t => t.NewEmail).HasColumnName("EmailTokenNewEmail").HasConversion(Converters.OptionalEmail).HasMaxLength(Email.MaxLength);
            token.Property(t => t.NewPasswordHash).HasColumnName("EmailTokenNewPasswordHash").HasMaxLength(256);
        });

        builder.OwnsMany(i => i.ExternalLogins, login =>
        {
            login.ToTable("ExternalLogins");
            login.WithOwner().HasForeignKey("IdentityId");
            login.HasKey("IdentityId", nameof(ExternalLogin.Provider));
            login.Property(l => l.Provider).HasConversion<string>().HasMaxLength(Converters.EnumLength);
            login.Property(l => l.Subject).HasMaxLength(ExternalLogin.SubjectMaxLength);
            login.Property(l => l.EmailAtLink).HasConversion(Converters.Email).HasMaxLength(Email.MaxLength);
            login.Property(l => l.LinkedAt);
            login.HasIndex(l => new { l.Provider, l.Subject }).IsUnique();
        });
        builder.Navigation(i => i.ExternalLogins).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(i => i.Sessions).WithOne().HasForeignKey(s => s.IdentityId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Sessions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
