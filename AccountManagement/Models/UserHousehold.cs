using System.ComponentModel.DataAnnotations;
using AccountManagement.Enums;
using Shared.Authorization;

namespace AccountManagement.Models;

public class UserHousehold : IEntity {
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid HouseholdId { get; set; }
    public Household Household { get; set; } = null!;

    
    [MaxLength(64)]
    public string Nickname { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    
    public UserFamilyRoleEnum UserHouseholdRole { get; set; } = UserFamilyRoleEnum.Guest;
    public FamilyFunctionEnum FamilyFunction { get; set; } = FamilyFunctionEnum.Other;
}