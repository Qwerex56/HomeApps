using HomeMix.Domain.Triggers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeMix.Infrastructure.Persistence.Configurations;

internal sealed class TriggerStateConfiguration : IEntityTypeConfiguration<TriggerState>
{
    public void Configure(EntityTypeBuilder<TriggerState> builder)
    {
        builder.ToTable("TriggerStates");
        builder.HasKey(s => new { s.InstallationId, s.TriggerId });
        builder.Property(s => s.TriggerId).HasMaxLength(64);
        builder.Property(s => s.LastError).HasMaxLength(TriggerState.MaxErrorLength);
    }
}
