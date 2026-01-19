using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Models;

public class RefreshToken : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
    
    public DateTime Created { get; set; }
    public DateTime Expires { get; set; }
    
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;
}