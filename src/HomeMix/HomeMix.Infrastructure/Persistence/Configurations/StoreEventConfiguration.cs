using HomeMix.Domain.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class StoreEventConfiguration : IEntityTypeConfiguration<StoreEvent>
{
    public void Configure(EntityTypeBuilder<StoreEvent> builder)
    {
        builder.ToTable("StoreEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.AddonId).HasMaxLength(31);
        builder.Property(e => e.Source).HasMaxLength(200);
        builder.Property(e => e.Version).HasMaxLength(64);
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(e => e.TargetKind).HasConversion<string>().HasMaxLength(16);

        builder.HasIndex(e => new { e.AddonId, e.Source, e.Kind, e.At });
    }
}
