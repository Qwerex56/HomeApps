using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Models;

public class Household {
    public Guid Id { get; init; } = Guid.NewGuid();

    [MaxLength(128)]
    public required string FamilyName { get; set; } = string.Empty;
    public DateTime Created { get; set; }

    public ICollection<UserHousehold> UserHouseholds { get; set; } = [];
    public ICollection<User>  Users { get; set; } = [];
}