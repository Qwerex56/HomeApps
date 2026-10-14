using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceName.Domain.Echoes;

namespace ServiceName.Infrastructure.Echoes;

internal sealed class EchoConfiguration : IEntityTypeConfiguration<Echo>
{
    public void Configure(EntityTypeBuilder<Echo> builder)
    {
        builder.ToTable("echoes");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Message).HasMaxLength(Echo.MaxMessageLength).IsRequired();
        builder.HasIndex(e => e.OwnerId);
    }
}
