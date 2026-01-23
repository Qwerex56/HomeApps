using System.ComponentModel.DataAnnotations;
using Shared.Authorization;

namespace AccountManagement.Models;

public class User : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;
    
    public UserSystemRoleEnum Role { get; set; } = UserSystemRoleEnum.SystemMember;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true; // Check for soft delete
 
    public RefreshToken? RefreshToken { get; set; }

    public UserCredential? UserCredential { get; set; }
    public ICollection<ExternalCredentials> ExternalCredentials { get; set; } = [];
    
    public ICollection<UserHousehold> UserHouseholds { get; set; } = [];
    public ICollection<Household>  Households { get; set; } = [];
}