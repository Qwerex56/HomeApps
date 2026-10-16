using Shared.Authorization;

namespace HouseholdService.Domain.Models;

public class User : Entity {
    public required string Username { get; set; }
    public required string Email { get; set; }
    public UserSystemRoleEnum Role { get; set; }

    public ICollection<Household> Households { get; set; } = [];
    public ICollection<UserHousehold> UserHouseholds { get; set; } = [];
}