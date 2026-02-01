using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AccountManagement.Data;

public class AccountDbContext : DbContext {
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    // tables
    public DbSet<User> Users { get; set; }
    public DbSet<UserHousehold> UserHouseholds { get; set; }
    public DbSet<Household> Households { get; set; }
    
    public DbSet<RefreshToken> JwtTokens { get; set; }
    public DbSet<UserCredential> UserCredentials { get; set; }
    public DbSet<ExternalCredentials> ExternalCredentials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>()
            .HasOne(e => e.UserCredential)
            .WithOne()
            .HasForeignKey<UserCredential>(e => e.UserId)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasMany(e => e.ExternalCredentials)
            .WithOne()
            .HasForeignKey(e => e.UserId)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasOne(e => e.RefreshToken)
            .WithOne()
            .HasForeignKey<RefreshToken>(e => e.UserId)
            .HasPrincipalKey<User>(e => e.Id)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasMany(e => e.Households)
            .WithMany(e => e.Users)
            .UsingEntity<UserHousehold>(
                r => r.HasOne<Household>(e => e.Household).WithMany(e => e.UserHouseholds),
                l => l.HasOne<User>(e => e.User).WithMany(e => e.UserHouseholds));
        
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