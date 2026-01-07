using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace AccountManagement.Models;

public class JwtToken {
    public Guid Id { get; init; } = Guid.NewGuid();    
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    
    [StringLength(SHA256.HashSizeInBytes)]
    public string TokenHash { get; set; } = string.Empty;
}