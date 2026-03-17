using Household.Api.Data;
using Household.Api.Repositories;
using Shared.Data;

namespace Household.Api.Extensions;

public class UnitOfWork : IUnitOfWork<HouseholdApiDbContext> {
    private readonly HouseholdApiDbContext _context;

    public HouseholdRepository HouseholdRepository { get; }
    public UserRepository UserRepository { get; }
    public UserHouseholdRepository UserHouseholdRepository { get; }

    public UnitOfWork(HouseholdApiDbContext context, HouseholdRepository householdRepository,
        UserRepository userRepository, UserHouseholdRepository userHouseholdRepository) {
        _context = context;
        HouseholdRepository = householdRepository;
        UserRepository = userRepository;
        UserHouseholdRepository = userHouseholdRepository;
    }

    public async Task SaveChangesAsync() {
        await _context.SaveChangesAsync();
    }
}