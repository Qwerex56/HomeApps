using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace AccountManagement.Models;

public class JwtToken : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    
    [StringLength(64)]
    public string TokenHash { get; set; } = string.Empty;
}