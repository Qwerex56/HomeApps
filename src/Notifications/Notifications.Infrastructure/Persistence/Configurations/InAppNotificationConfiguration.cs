using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.InApp;

namespace Notifications.Infrastructure.Persistence.Configurations;

internal sealed class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        builder.ToTable("InAppNotifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Source).HasMaxLength(InAppNotification.MaxSourceLength);
        builder.Property(n => n.Category).HasMaxLength(InAppNotification.MaxCategoryLength);
        builder.Property(n => n.Tag).HasMaxLength(InAppNotification.MaxTagLength);
        builder.Property(n => n.Title).HasMaxLength(InAppNotification.MaxTitleLength);
        builder.Property(n => n.Body);
        builder.Property(n => n.Url).HasMaxLength(InAppNotification.MaxUrlLength);

        builder.HasIndex(n => new { n.UserId, n.CreatedAt }).IsDescending(false, true).HasDatabaseName("IX_InAppNotifications_UserId_CreatedAt");
        builder.HasIndex(n => n.UserId).HasFilter("\"ReadAt\" IS NULL").HasDatabaseName("IX_InAppNotifications_Unread");
    }
}
