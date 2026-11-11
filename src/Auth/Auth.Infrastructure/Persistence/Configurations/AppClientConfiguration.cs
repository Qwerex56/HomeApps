using Auth.Domain.ConnectedApps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

internal sealed class AppClientConfiguration : IEntityTypeConfiguration<AppClient>
{
    public void Configure(EntityTypeBuilder<AppClient> builder)
    {
        builder.ToTable("AppClients");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(64).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(AppClient.NameMaxLength);
        builder.Ignore(c => c.RedirectUris);
        builder.PrimitiveCollection<List<string>>("_redirectUris").HasColumnName("RedirectUris");
    }
}
