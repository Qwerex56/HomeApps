using Accounts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

internal sealed class HistoryEntryConfiguration : IEntityTypeConfiguration<HistoryEntry>
{
    public void Configure(EntityTypeBuilder<HistoryEntry> builder)
    {
        builder.ToTable("AccountHistory");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.Action).HasConversion<string>().HasMaxLength(Converters.EnumLength);
        builder.Property(h => h.Reason).HasMaxLength(Disabling.ReasonMaxLength);
        builder.Property(h => h.Details).HasMaxLength(256);

        // Newest first, page by page with the "before" cursor (Q6).
        builder.HasIndex(h => new { h.UserId, h.At, h.Id }).IsDescending(false, true, true);
    }
}
