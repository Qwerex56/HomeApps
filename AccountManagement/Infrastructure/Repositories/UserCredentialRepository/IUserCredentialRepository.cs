using AccountService.Domain.Models;

namespace AccountService.Infrastructure.Repositories.UserCredentialRepository;

public interface IUserCredentialRepository : IRepository<UserCredential> {
    public Task<UserCredential?> GetByEmailAsync(string email);
}