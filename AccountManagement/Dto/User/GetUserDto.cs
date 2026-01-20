namespace AccountManagement.Dto.User;

public class GetUserDto {
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}