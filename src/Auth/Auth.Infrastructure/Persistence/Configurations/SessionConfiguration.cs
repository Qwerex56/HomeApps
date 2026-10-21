using Auth.Domain.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Method).HasConversion<string>().HasMaxLength(Converters.EnumLength);

        builder.ComplexProperty(s => s.Client, client =>
        {
            client.Property(c => c.ClientId).HasColumnName("ClientId").HasMaxLength(ClientInfo.ClientIdMaxLength);
            client.Property(c => c.UserAgent).HasColumnName("UserAgent").HasMaxLength(ClientInfo.UserAgentMaxLength);
        });

        builder.HasMany(s => s.RefreshTokens).WithOne().HasForeignKey(t => t.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property<uint>("Version").IsRowVersion();
        builder.Property(t => t.TokenHash).HasMaxLength(Converters.TokenHashLength);
        builder.HasIndex(t => t.TokenHash).IsUnique();
    }
}
