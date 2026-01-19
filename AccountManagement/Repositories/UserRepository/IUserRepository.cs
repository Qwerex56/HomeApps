using AccountManagement.Models;

namespace AccountManagement.Repositories.UserRepository;

public interface IUserRepository : ISimpleRepository<User, Guid> {
    public Task CreateUserCredentialsAsync(UserCredential userCredential);
    
    public Task CreateExternalCredentialsAsync(ExternalCredentials externalCredentials);
}