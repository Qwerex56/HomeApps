using Jarvis.Domain.Runs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jarvis.Infrastructure.Persistence.Configurations;

internal sealed class AssistantRunConfiguration : IEntityTypeConfiguration<AssistantRun>
{
    public void Configure(EntityTypeBuilder<AssistantRun> builder)
    {
        builder.ToTable("Runs");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Channel).HasMaxLength(32);
        builder.Property(r => r.Reference).HasMaxLength(200);
        builder.Property(r => r.Model).HasMaxLength(100);
        builder.Property(r => r.CostUsd).HasPrecision(12, 6);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(r => r.StartedAt);
        builder.HasIndex(r => r.UserId);
        builder.Ignore(r => r.IsRunning);
    }
}
