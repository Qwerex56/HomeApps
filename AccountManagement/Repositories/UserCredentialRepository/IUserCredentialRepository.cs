using AccountManagement.Models;

namespace AccountManagement.Repositories.UserCredentialRepository;

public interface IUserCredentialRepository : IRepository<UserCredential> {
    public Task<UserCredential?> GetByEmailAsync(string email);
}