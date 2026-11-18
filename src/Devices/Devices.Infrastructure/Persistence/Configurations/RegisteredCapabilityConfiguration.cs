using Devices.Domain.Capabilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devices.Infrastructure.Persistence.Configurations;

internal sealed class RegisteredCapabilityConfiguration : IEntityTypeConfiguration<RegisteredCapability>
{
    public void Configure(EntityTypeBuilder<RegisteredCapability> builder)
    {
        builder.ToTable("RegisteredCapabilities");
        builder.HasKey(c => c.Name);
        builder.Property(c => c.Name).HasMaxLength(70);
        builder.Property(c => c.DefinitionJson).HasColumnType("jsonb");
        builder.Ignore(c => c.Definition);
    }
}
