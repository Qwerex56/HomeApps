using Devices.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devices.Infrastructure.Persistence.Configurations;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.Name).HasMaxLength(Device.MaxNameLength);
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(d => new { d.OwnerId, d.Name }).IsUnique();
        builder.Ignore(d => d.IsDisabled);

        builder.OwnsMany(d => d.Actions, action =>
        {
            action.ToTable("DeviceActions");
            action.WithOwner().HasForeignKey("DeviceId");
            action.HasKey("DeviceId", nameof(DeviceAction.Name));
            action.Property(a => a.Name).HasMaxLength(DeviceAction.MaxNameLength);
            action.Property(a => a.Description).HasMaxLength(DeviceAction.MaxDescriptionLength);
            action.Property(a => a.InputSchema).HasMaxLength(DeviceAction.MaxSchemaLength);
            action.Ignore(a => a.NeedsConfirmation);
        });
        builder.Navigation(d => d.Actions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
