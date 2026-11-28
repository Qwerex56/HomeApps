using Jarvis.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jarvis.Infrastructure.Persistence.Configurations;

internal sealed class JarvisLlmSettingsConfiguration : IEntityTypeConfiguration<JarvisLlmSettings>
{
    public void Configure(EntityTypeBuilder<JarvisLlmSettings> builder)
    {
        builder.ToTable("JarvisLlmSettings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.BaseUrl).HasMaxLength(500);
        builder.Property(s => s.Model).HasMaxLength(200);
        builder.Property(s => s.ApiKeyProtected).HasMaxLength(2000);
    }
}
