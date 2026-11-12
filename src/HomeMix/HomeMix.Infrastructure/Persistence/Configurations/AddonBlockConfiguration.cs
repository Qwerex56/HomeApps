using HomeMix.Domain.Blocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class AddonBlockConfiguration : IEntityTypeConfiguration<AddonBlock>
{
    public void Configure(EntityTypeBuilder<AddonBlock> builder)
    {
        builder.ToTable("Blocks");
        builder.HasKey(b => b.Value);
        builder.Property(b => b.Value).HasMaxLength(64);
        builder.Property(b => b.Reason).HasMaxLength(AddonBlock.MaxReasonLength);
    }
}
