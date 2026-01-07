using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Data;

public class AccountDbContext : DbContext {
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) {
    }

    // tables
    public DbSet<Household> Households { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Accounts)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);

        modelBuilder.Entity<User>()
            .HasMany(e => e.Households)
            .WithMany(e => e.Users)
            .UsingEntity<UserHousehold>(
                r => r.HasOne(e => e.Household).WithMany(e => e.UserHouseholds).HasForeignKey(e => e.HouseholdId),
                l => l.HasOne(e => e.User).WithMany(e => e.UserHouseholds).HasForeignKey(e => e.UserId)
            );

        modelBuilder.Entity<User>()
            .HasOne(e => e.JwtToken)
            .WithOne(e => e.User)
            .HasForeignKey<JwtToken>(e => e.UserId);
    }
}