using Auth.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Infrastructure.Persistence.Configurations;

internal sealed class DeviceIdentityConfiguration : IEntityTypeConfiguration<DeviceIdentity>
{
    public void Configure(EntityTypeBuilder<DeviceIdentity> builder)
    {
        builder.ToTable("DeviceIdentities");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).HasMaxLength(DeviceIdentity.NameMaxLength);
        builder.Property(d => d.SecretHash).HasMaxLength(64);
        builder.Ignore(d => d.IsActive);
        builder.HasIndex(d => d.HouseholdId);
    }
}
