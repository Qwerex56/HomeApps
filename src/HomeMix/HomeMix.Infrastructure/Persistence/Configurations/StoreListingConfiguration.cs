using HomeMix.Domain.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class StoreListingConfiguration : IEntityTypeConfiguration<StoreListing>
{
    public void Configure(EntityTypeBuilder<StoreListing> builder)
    {
        builder.ToTable("StoreListings");
        builder.HasKey(l => l.Key);
        builder.Property(l => l.Key).HasMaxLength(ListingKey.MaxLength + 8);
        builder.Property(l => l.AddonId).HasMaxLength(31);
        builder.Property(l => l.Repository).HasMaxLength(200);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(l => l.PackageSha256).HasMaxLength(64);
        builder.Property(l => l.ReleaseTag).HasMaxLength(100);
        builder.Property(l => l.Readme).HasMaxLength(StoreListing.MaxReadmeLength + 8);
        builder.Property(l => l.Problem).HasMaxLength(64);

        builder.HasIndex(l => new { l.Status, l.SubmittedAt });
        builder.HasIndex(l => new { l.SubmittedBy, l.LastSubmittedAt });
        builder.HasIndex(l => l.RefreshedAt);
    }
}
