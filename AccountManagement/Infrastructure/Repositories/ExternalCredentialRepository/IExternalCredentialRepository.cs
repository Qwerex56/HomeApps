using AccountService.Domain.Models;

namespace AccountService.Infrastructure.Repositories.ExternalCredentialRepository;

public interface IExternalCredentialRepository : IRepository<ExternalCredentials> {
    public Task<ExternalCredentials?> GetExternalCredentialByProviderId(string providerId);
}