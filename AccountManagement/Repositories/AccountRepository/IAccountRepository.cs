using AccountManagement.Models;

namespace AccountManagement.Repositories.AccountRepository;

public interface IAccountRepository : ISimpleRepository<Account, Guid> {
}