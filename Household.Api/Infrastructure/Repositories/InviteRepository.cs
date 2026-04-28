using HouseholdService.Domain.Models;
using HouseholdService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HouseholdService.Infrastructure.Repositories;

public class InviteRepository : IRepository<Invite> {
    private readonly HouseholdApiDbContext _context;

    public InviteRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public async Task<Invite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        return await _context.Invites
            .AsNoTracking()
            .FirstOrDefaultAsync(invite => invite.Id == id, cancellationToken);
    }

    public Task AddAsync(Invite entity) {
        _context.Invites.Add(entity);
        
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invite entity) {
        _context.Invites.Update(entity);
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Invite entity) {
        _context.Invites.Remove(entity);
        
        return Task.CompletedTask;
    }

    public async Task<Invite?> GetByCodeAsync(string code) {
        var invite = await _context.Invites
            .FirstOrDefaultAsync(inv => inv.InviteCode == code);

        return invite;
    }

    public async Task<IEnumerable<Invite>> GetAllInHousehold(Guid householdId) {
        var invites = _context
            .Invites
            .AsNoTracking()
            .Where(invite => invite.HouseholdId == householdId)
            .Select(invite => invite);

        return await invites.ToListAsync();
    }
}