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
        // Names are unique ignoring case: a case-insensitive (non-deterministic ICU) collation does it in the index too.
        builder.Property(d => d.Name).HasMaxLength(DeviceName.MaxLength).UseCollation(DevicesDbContext.CaseInsensitive);
        builder.Property(d => d.Slug).HasMaxLength(DeviceSlug.MaxLength);
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Purpose).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.AdminNotes).HasMaxLength(Device.MaxNotesLength);
        builder.Property(d => d.Descriptor).HasColumnType("jsonb");
        builder.Property(d => d.Model).HasMaxLength(100);
        builder.Property(d => d.DescriptorVersion).HasMaxLength(50);
        builder.HasIndex(d => new { d.OwnerId, d.Name }).IsUnique().HasFilter("\"HouseholdId\" IS NULL");
        builder.HasIndex(d => new { d.HouseholdId, d.Name }).IsUnique().HasFilter("\"HouseholdId\" IS NOT NULL");
        builder.HasIndex(d => new { d.OwnerId, d.Slug }).IsUnique().HasFilter("\"HouseholdId\" IS NULL");
        builder.HasIndex(d => new { d.HouseholdId, d.Slug }).IsUnique().HasFilter("\"HouseholdId\" IS NOT NULL");
        builder.HasIndex(d => d.PairingCodeHash).IsUnique().HasFilter("\"PairingCodeHash\" IS NOT NULL");
        builder.Property(d => d.PairingCodeHash).HasMaxLength(64);
        builder.Ignore(d => d.IsDisabled);
        builder.Ignore(d => d.IsHouseholdDevice);

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
