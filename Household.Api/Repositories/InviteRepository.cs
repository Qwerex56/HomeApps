using Household.Api.Data;
using Household.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Household.Api.Repositories;

public class InviteRepository : IRepository<Invite> {
    private readonly HouseholdApiDbContext _context;

    public InviteRepository(HouseholdApiDbContext context) {
        _context = context;
    }

    public Task<Invite?> GetByIdAsync(Guid id) {
        throw new NotImplementedException();
    }

    public Task AddAsync(Invite entity) {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Invite entity) {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(Invite entity) {
        throw new NotImplementedException();
    }
    
    public async Task<Invite?> GetByCodeAsync(string code) {
        var invite = await _context.Invites
            .FirstOrDefaultAsync(inv => inv.InviteCode == code);
        
        return invite;
    }
}