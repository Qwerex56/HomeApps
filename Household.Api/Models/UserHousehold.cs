using System.ComponentModel.DataAnnotations;
using Shared.Authorization;

namespace Household.Api.Models;

public class UserHousehold : Entity {
    // Navigations
    public required Guid UserId { get; set; }
    public required Guid HouseholdId { get; set; }

    public User User { get; set; } = null!;
    public Household Household { get; set; } = null!;
    
    
    public required UserFamilyRoleEnum UserFamilyRole { get; set; }
    
    [MaxLength(32)]
    public required string Nickname { get; set; }
}