using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Recipients;

namespace Notifications.Infrastructure.Persistence.Configurations;

internal sealed class RecipientConfiguration : IEntityTypeConfiguration<Recipient>
{
    public void Configure(EntityTypeBuilder<Recipient> builder)
    {
        builder.ToTable("Recipients");
        builder.HasKey(r => r.UserId);
        builder.Property(r => r.UserId).ValueGeneratedNever();
        builder.Property(r => r.ShowPreview);

        builder.OwnsOne(r => r.QuietHours, quiet =>
        {
            quiet.Property(q => q.From).HasColumnName("QuietFrom");
            quiet.Property(q => q.To).HasColumnName("QuietTo");
            quiet.Property(q => q.TimeZone).HasColumnName("QuietTimeZone").HasMaxLength(64);
        });

        builder.OwnsMany(r => r.Devices, device =>
        {
            device.ToTable("PushDevices");
            device.WithOwner().HasForeignKey("UserId");
            device.HasKey(d => d.Id);
            device.Property(d => d.Id).ValueGeneratedNever();
            device.Property(d => d.Endpoint).HasMaxLength(PushTarget.MaxEndpointLength);
            device.Property(d => d.P256dh).HasMaxLength(200);
            device.Property(d => d.Auth).HasMaxLength(200);
            device.Property(d => d.UserAgent).HasMaxLength(512);
            device.HasIndex(d => d.Endpoint);
            device.Ignore(d => d.Target);
        });
        builder.Navigation(r => r.Devices).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(r => r.Mutes, mute =>
        {
            mute.ToTable("Mutes");
            mute.WithOwner().HasForeignKey("UserId");
            mute.Property<int>("Id");
            mute.HasKey("Id");
            mute.Property(m => m.Source).HasMaxLength(Mute.MaxSourceLength);
        });
        builder.Navigation(r => r.Mutes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
