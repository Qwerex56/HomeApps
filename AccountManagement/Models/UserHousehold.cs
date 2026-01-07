using System.ComponentModel.DataAnnotations;
using AccountManagement.Enums;

namespace AccountManagement.Models;

public class UserHousehold {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid HouseholdId { get; set; }
    public Household Household { get; set; } = null!;

    public HouseholdRoleEnum UserHouseholdRole { get; set; } = HouseholdRoleEnum.Guest;
    public DateTime JoinDate { get; set; }
    
    [MaxLength(64)]
    public string Nickname { get; set; } = string.Empty;
    public FamilyFunctionEnum FamilyFunction { get; set; } = FamilyFunctionEnum.Other;
}