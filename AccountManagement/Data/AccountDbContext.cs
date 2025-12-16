using AccountManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountManagement.Data;

public class AccountDbContext : DbContext {
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }
    
    // tables
    public DbSet<Household> Households { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Account> Accounts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>()
            .HasOne(u => u.Household)
            .WithMany(h => h.Members)
            .HasForeignKey(u => u.HouseholdId);
        
        modelBuilder.Entity<User>()
            .HasMany(u => u.Accounts)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId);
    }
}