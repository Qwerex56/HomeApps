using HomeMix.Domain.Packages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class AddonPackageConfiguration : IEntityTypeConfiguration<AddonPackage>
{
    public void Configure(EntityTypeBuilder<AddonPackage> builder)
    {
        builder.ToTable("Packages");
        builder.HasKey(p => p.Sha256);
        builder.Property(p => p.Sha256).HasMaxLength(64);
        builder.Property(p => p.AddonId).HasMaxLength(31);
        builder.Property(p => p.Version).HasMaxLength(64);
        builder.Property(p => p.ManifestJson).HasColumnType("jsonb");
        builder.Property(p => p.Repository).HasMaxLength(300);
        builder.Property(p => p.Tag).HasMaxLength(100);
        builder.Ignore(p => p.Manifest);
        builder.Ignore(p => p.IsOfficial);
        builder.HasIndex(p => p.AddonId);
    }
}
