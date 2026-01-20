namespace AccountManagement.Models;

public class ExternalCredentials : IEntity {
    public Guid UserId { get; set; }

    public string ProviderName { get; set; } = string.Empty;
    public string ProviderId { get; set; }  = string.Empty;
    
    public DateTime CreatedAt { get; set; }
}