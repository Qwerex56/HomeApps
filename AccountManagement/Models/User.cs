using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Models;

public class User : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true; // Check for soft delete

    public ICollection<UserHousehold> UserHouseholds { get; set; } = [];
    public ICollection<Household>  Households { get; set; } = [];

    public RefreshToken RefreshToken { get; set; } = null!;
}