using HomeMix.Domain.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class AddonDocumentConfiguration : IEntityTypeConfiguration<AddonDocument>
{
    public void Configure(EntityTypeBuilder<AddonDocument> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Scope).HasConversion<string>().HasMaxLength(16);
        builder.Property(d => d.Collection).HasMaxLength(DataNames.MaxLength);
        builder.Property(d => d.Key).HasMaxLength(DataNames.MaxLength);
        builder.Property(d => d.Value).HasColumnType("jsonb");
        builder.Property(d => d.Version).IsConcurrencyToken();

        // OwnerUserId is null in the shared scope, and those must be unique too.
        builder.HasIndex(d => new { d.InstallationId, d.Scope, d.OwnerUserId, d.Collection, d.Key }).IsUnique().AreNullsDistinct(false);
    }
}
