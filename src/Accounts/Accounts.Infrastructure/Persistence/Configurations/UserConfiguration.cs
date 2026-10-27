using Accounts.Domain.Common;
using Accounts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

/// <summary>Profile, registration and disabling are columns of <c>Users</c>: one row per account, nothing repeats (3NF).</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(u => u.DomainEvents);

        builder.Property(u => u.Email).HasConversion(Converters.Email).HasMaxLength(Email.MaxLength);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.HasIndex(u => u.Role);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.HasIndex(u => u.Status);

        builder.ComplexProperty(u => u.Profile, profile =>
        {
            profile.Property(p => p.DisplayName).HasColumnName("DisplayName")
                .HasConversion(Converters.DisplayName).HasMaxLength(DisplayName.MaxLength);
            profile.Property(p => p.FirstName).HasColumnName("FirstName")
                .HasConversion(Converters.PersonName).HasMaxLength(PersonName.MaxLength);
            profile.Property(p => p.LastName).HasColumnName("LastName")
                .HasConversion(Converters.PersonName).HasMaxLength(PersonName.MaxLength);
            profile.Property(p => p.PreferredName).HasColumnName("PreferredName")
                .HasConversion(Converters.PersonName).HasMaxLength(PersonName.MaxLength);
            profile.Property(p => p.AvatarUrl).HasColumnName("AvatarUrl")
                .HasConversion(Converters.AvatarUrl).HasMaxLength(AvatarUrl.MaxLength);
            profile.Property(p => p.Locale).HasColumnName("Locale")
                .HasConversion(Converters.Locale).HasMaxLength(Converters.CodeLength);
            profile.Property(p => p.TimeZone).HasColumnName("TimeZone")
                .HasConversion(Converters.TimeZone).HasMaxLength(Converters.CodeLength);
            profile.Property(p => p.DateOfBirth).HasColumnName("DateOfBirth");
        });

        builder.ComplexProperty(u => u.Disabling, disabling =>
        {
            disabling.Property(d => d.At).HasColumnName("DisabledAt");
            disabling.Property(d => d.ByUserId).HasColumnName("DisabledBy");
            disabling.Property(d => d.Reason).HasColumnName("DisabledReason").HasMaxLength(Disabling.ReasonMaxLength);
        });

        builder.ComplexProperty(u => u.Registration, registration =>
        {
            registration.Property(r => r.Source).HasColumnName("RegistrationSource")
                .HasConversion<string>().HasMaxLength(Converters.EnumLength);
            registration.Property(r => r.RegisteredAt).HasColumnName("RegisteredAt");
            registration.Property(r => r.ViaReferral).HasColumnName("ViaReferral");
            registration.Property(r => r.InvitedByEmail).HasColumnName("InvitedByEmail").HasMaxLength(Email.MaxLength);
            registration.Property(r => r.InvitedEmail).HasColumnName("InvitedEmail").HasMaxLength(Email.MaxLength);
            registration.Property(r => r.ClientApp).HasColumnName("ClientApp").HasMaxLength(Registration.ClientAppMaxLength);
            registration.Property(r => r.UserAgent).HasColumnName("UserAgent").HasMaxLength(Registration.UserAgentMaxLength);
            registration.Property(r => r.Locale).HasColumnName("RegistrationLocale").HasMaxLength(Registration.LocaleMaxLength);
        });

        // Commands never load the history (Q6); new entries are inserted because their keys come from the domain.
        builder.HasMany(u => u.History).WithOne().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
