namespace AccountService.Application.Dto.User;

public class CreatedUserDto {
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
}