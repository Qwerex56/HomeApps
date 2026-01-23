using AccountManagement.Models;

namespace AccountManagement.Repositories.ExternalCredentialRepository;

public interface IExternalCredentialRepository : IRepository<ExternalCredentials> {
    public Task<ExternalCredentials?> GetExternalCredentialByProviderId(string providerId);
}