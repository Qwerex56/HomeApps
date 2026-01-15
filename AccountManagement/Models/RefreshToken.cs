using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Models;

public class RefreshToken : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;
}