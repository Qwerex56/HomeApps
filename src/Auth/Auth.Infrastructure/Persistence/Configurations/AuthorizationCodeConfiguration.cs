using Auth.Domain.ConnectedApps;
using Auth.Domain.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthorizationCodeConfiguration : IEntityTypeConfiguration<AuthorizationCode>
{
    public void Configure(EntityTypeBuilder<AuthorizationCode> builder)
    {
        builder.ToTable("AuthorizationCodes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.CodeHash).HasMaxLength(Converters.TokenHashLength);
        builder.HasIndex(c => c.CodeHash).IsUnique();
        builder.Property(c => c.ClientId).HasMaxLength(64);
        builder.Property(c => c.RedirectUri).HasMaxLength(AppClient.RedirectUriMaxLength);
        builder.Property(c => c.CodeChallenge).HasMaxLength(Pkce.MaxLength);
        builder.Property(c => c.Audience).HasMaxLength(Session.AudienceMaxLength);

        // Redeeming twice at the same moment: only one wins, the other fails on the version (OAuth 2.1, single use).
        builder.Property<uint>("Version").IsRowVersion();
    }
}
