using System.Text.Json;
using HomeMix.Domain.Boards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class WidgetBoardConfiguration : IEntityTypeConfiguration<WidgetBoard>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<WidgetBoard> builder)
    {
        builder.ToTable("Boards");
        builder.HasKey(b => new { b.HouseholdId, b.Place });
        builder.Property(b => b.Place).HasMaxLength(16);

        builder.Property(b => b.UpdatedAt).IsConcurrencyToken();

        // Always read and written as a whole: one jsonb column.
        builder.Property<List<BoardItem>>("_items").HasColumnName("Items").HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                v => JsonSerializer.Deserialize<List<BoardItem>>(v, Json) ?? new List<BoardItem>(),
                new ValueComparer<List<BoardItem>>(
                    (a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, i) => HashCode.Combine(h, i.GetHashCode())), v => v.ToList()))
            .HasDefaultValueSql("'[]'::jsonb");
        builder.Ignore(b => b.Items);
    }
}
