using HouseholdService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseholdService.Infrastructure.Data;

public class HouseholdApiDbContext : DbContext {
    public HouseholdApiDbContext(DbContextOptions options) : base(options) { }

    // Tables
    public DbSet<User> Users { get; set; }
    public DbSet<HouseholdService.Domain.Models.Household> Households { get; set; }
    public DbSet<UserHousehold> UserHouseholds { get; set; }
    public DbSet<Invite> Invites { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>()
            .HasMany(e => e.Households)
            .WithMany(e => e.Users)
            .UsingEntity<UserHousehold>(
                configureRight => configureRight
                    .HasOne<HouseholdService.Domain.Models.Household>(e => e.Household)
                    .WithMany(e => e.UserHouseholds),
                configureLeft => configureLeft
                    .HasOne<User>(e => e.User)
                    .WithMany(e => e.UserHouseholds)
            );

        base.OnModelCreating(modelBuilder);
    }
}