using HomeMix.Domain.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class StoreHeartConfiguration : IEntityTypeConfiguration<StoreHeart>
{
    public void Configure(EntityTypeBuilder<StoreHeart> builder)
    {
        builder.ToTable("StoreHearts");
        builder.HasKey(h => new { h.ListingKey, h.UserId });
        builder.Property(h => h.ListingKey).HasMaxLength(ListingKey.MaxLength + 8);
        builder.HasIndex(h => h.UserId);
    }
}
