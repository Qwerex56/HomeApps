using AccountManagement.Models;

namespace AccountManagement.Repositories.UserHouseholdRepository;

public interface IUserHouseHoldRepository : ICompositeRepository<UserHousehold, Guid, Guid> {
}