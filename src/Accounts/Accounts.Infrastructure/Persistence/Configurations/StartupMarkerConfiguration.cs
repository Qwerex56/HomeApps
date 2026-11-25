using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

internal sealed class StartupMarkerConfiguration : IEntityTypeConfiguration<StartupMarker>
{
    public void Configure(EntityTypeBuilder<StartupMarker> builder)
    {
        builder.ToTable("StartupMarkers");
        builder.HasKey(m => m.Name);
        builder.Property(m => m.Name).HasMaxLength(100);
    }
}
