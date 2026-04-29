using System.ComponentModel.DataAnnotations;
using Shared.Authorization;

namespace HouseholdService.Domain.Models;

public class Household : Entity {
    [MaxLength(32)]
    public required string Name { get; set; }

    [MaxLength(120)]
    public string Description { get; set; } = string.Empty;

    public ICollection<User> Users { get; init; } = [];
    public ICollection<UserHousehold> UserHouseholds { get; init; } = [];

    public void AddHouseholdMember(Guid requestUserId,
        UserFamilyRoleEnum userFamilyRole = UserFamilyRoleEnum.FamilyMember) {
        var userHousehold = new UserHousehold {
            UserId = requestUserId,
            HouseholdId = Id,

            UserFamilyRole = userFamilyRole,
            Nickname = Name
        };
        
        UserHouseholds.Add(userHousehold);
    }

    public void RemoveHouseholdMember(Guid requestUserId) {
        var userHousehold = UserHouseholds.SingleOrDefault(household => household.UserId == requestUserId);
        
        if (userHousehold is null) return;
        UserHouseholds.Remove(userHousehold);
    }

    public void RemoveHouseholdMember(UserHousehold userHousehold) {
        UserHouseholds.Remove(userHousehold);
    }

    public static Household Create(Guid ownerId, string name, string description) {
        var household = new Household {
            Name = name,
            Description = description
        };
        
        household.AddHouseholdMember(ownerId, UserFamilyRoleEnum.FamilyOwner);
        
        return household;
    }

    public void Update(Guid requestEditorId, string requestNewName, string requestNewDescription) {
        Name = requestNewName;
        Description = requestNewDescription;
    }

    public bool HasUser(Guid userId) {
        return Users.Any(u => u.Id == userId);
    }
}