using Accounts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence.Configurations;

internal sealed class ErasedUserConfiguration : IEntityTypeConfiguration<ErasedUser>
{
    public void Configure(EntityTypeBuilder<ErasedUser> builder)
    {
        builder.ToTable("ErasedUsers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
    }
}
