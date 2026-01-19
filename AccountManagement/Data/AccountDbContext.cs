using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AccountManagement.Data;

public class AccountDbContext : DbContext {
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) {
    }

    // tables
    public DbSet<Household> Households { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserHousehold> UserHouseholds { get; set; }
    public DbSet<RefreshToken> JwtTokens { get; set; }
    public DbSet<UserCredential> UserCredentials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>()
            .HasMany(e => e.Households)
            .WithMany(e => e.Users)
            .UsingEntity<UserHousehold>(
                r => r.HasOne(e => e.Household).WithMany(e => e.UserHouseholds).HasForeignKey(e => e.HouseholdId),
                l => l.HasOne(e => e.User).WithMany(e => e.UserHouseholds).HasForeignKey(e => e.UserId)
            );

        modelBuilder.Entity<User>()
            .HasOne(e => e.RefreshToken)
            .WithOne(e => e.User)
            .HasForeignKey<RefreshToken>(e => e.UserId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()) {
            var id = entityType.FindProperty("Id");
            
            if (id is null) {
                continue;
            }
            
            id.SetColumnType("uuid");
            id.ValueGenerated = ValueGenerated.Never;
        }
    }
}