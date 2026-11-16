using System.Text.Json;
using HomeMix.Domain.Installations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class InstallationConfiguration : IEntityTypeConfiguration<Installation>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Installation> builder)
    {
        builder.ToTable("Installations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.AddonId).HasMaxLength(31);
        builder.Property(i => i.PackageSha256).HasMaxLength(64);
        builder.Property(i => i.Version).HasMaxLength(64);
        builder.Property(i => i.TargetKind).HasConversion<string>().HasMaxLength(16);
        builder.Ignore(i => i.Target);
        builder.Ignore(i => i.IsActive);

        builder.Property<List<string>>("_capabilities").HasColumnName("Capabilities");
        builder.Property<List<string>>("_network").HasColumnName("Network");
        builder.Ignore(i => i.Capabilities);
        builder.Ignore(i => i.Network);

        // A handful of choices, always read with the installation: one jsonb column.
        builder.Property<List<WidgetChoice>>("_widgets").HasColumnName("Widgets").HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<List<WidgetChoice>>(v, Json) ?? new List<WidgetChoice>(),
                new ValueComparer<List<WidgetChoice>>(
                    (a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, c) => HashCode.Combine(h, c.GetHashCode())), v => v.ToList()))
            .HasDefaultValueSql("'[]'::jsonb");
        builder.Ignore(i => i.Widgets);

        // One active installation of an add-on per household or account; removed ones stay for 30 days.
        builder.HasIndex(i => new { i.AddonId, i.TargetKind, i.TargetId }).IsUnique().HasFilter("\"RemovedAt\" IS NULL");
        builder.HasIndex(i => new { i.TargetKind, i.TargetId });
        builder.HasIndex(i => i.PackageSha256);
    }
}
