namespace AccountManagement.Dto.User;

public class CreateUserExternalDto {
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
}