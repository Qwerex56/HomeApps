using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Models;

public class Household : IEntity {
    public Guid Id { get; init; } = Guid.NewGuid();

    [MaxLength(128)]
    public required string FamilyName { get; set; } = string.Empty;
    public DateTime Created { get; set; }

    public ICollection<UserHousehold> UserHouseholds { get; } = [];
    public ICollection<User> Users { get; } = [];
}