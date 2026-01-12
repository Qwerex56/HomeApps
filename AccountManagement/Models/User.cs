using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Models;

public class User : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    public ICollection<UserHousehold> UserHouseholds { get; set; } = [];
    public ICollection<Household>  Households { get; set; } = [];
    
    public ICollection<Account> Accounts { get; set; } = [];

    public JwtToken JwtToken { get; set; } = null!;
}