using Devices.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devices.Infrastructure.Persistence.Configurations;

internal sealed class DeviceStateConfiguration : IEntityTypeConfiguration<DeviceState>
{
    public void Configure(EntityTypeBuilder<DeviceState> builder)
    {
        builder.ToTable("DeviceStates");
        builder.HasKey(s => s.DeviceId);
        builder.Property(s => s.DeviceId).ValueGeneratedNever();
        builder.Property(s => s.Values).HasColumnType("jsonb");
        builder.Property(s => s.PendingSettings).HasColumnType("text");
        builder.Property(s => s.Firmware).HasMaxLength(StatusReport.MaxFirmwareLength);
        builder.Property(s => s.Error).HasMaxLength(StatusReport.MaxErrorLength);
        builder.HasOne<Device>().WithOne().HasForeignKey<DeviceState>(s => s.DeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}
