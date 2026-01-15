namespace AccountManagement.Models;

public class ExternalCredentials {
    public Guid Id { get; init; }

    public Guid UserId { get; init; }
    public User User { get; init; }

    public string ProviderName { get; init; }
    public string ProviderId { get; init; }
}