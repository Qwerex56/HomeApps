using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountManagement.Models;

public class RefreshToken : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime Created { get; set; }
    public DateTime Expires { get; set; }

    [StringLength(64)] 
    public string TokenHash { get; set; } = string.Empty;
    
    public Guid UserId { get; set; }
}