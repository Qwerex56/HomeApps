namespace AccountManagement.Dto.HouseholdDto;

public class HouseholdDto {
    public Guid Id { get; init; }
    
    public string FamilyName { get; init; }
    public DateTime Created { get; init; }
    
    public int UsersCount { get; init; }
}