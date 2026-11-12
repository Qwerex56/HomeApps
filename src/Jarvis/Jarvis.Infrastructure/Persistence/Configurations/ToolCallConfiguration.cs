using Jarvis.Domain.Runs;
using Jarvis.Domain.ToolCalls;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jarvis.Infrastructure.Persistence.Configurations;

internal sealed class ToolCallConfiguration : IEntityTypeConfiguration<ToolCall>
{
    public void Configure(EntityTypeBuilder<ToolCall> builder)
    {
        builder.ToTable("ToolCalls");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Tool).HasMaxLength(128);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasOne<AssistantRun>().WithMany().HasForeignKey(c => c.RunId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.RunId);
    }
}
