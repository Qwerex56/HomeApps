using Devices.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Devices.Infrastructure.Persistence.Configurations;

internal sealed class DeviceEventConfiguration : IEntityTypeConfiguration<DeviceEvent>
{
    public void Configure(EntityTypeBuilder<DeviceEvent> builder)
    {
        builder.ToTable("DeviceEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Name).HasMaxLength(DeviceEvent.MaxNameLength);
        builder.Property(e => e.Data).HasColumnType("jsonb");
        builder.HasIndex(e => new { e.DeviceId, e.OccurredAt });
        builder.HasIndex(e => new { e.HouseholdId, e.OccurredAt });
        builder.HasOne<Device>().WithMany().HasForeignKey(e => e.DeviceId).OnDelete(DeleteBehavior.Cascade);
    }
}
