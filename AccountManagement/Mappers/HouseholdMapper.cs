using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Models;
using Riok.Mapperly.Abstractions;

namespace AccountManagement.Mappers;

[Mapper]
public static partial class HouseholdMapper {
    [MapperIgnoreSource(nameof(Household.UserHouseholds))]
    [MapProperty(nameof(Household.Users), nameof(HouseholdDto.UsersCount), Use = nameof(MapUserCount))]
    public static partial HouseholdDto ToHouseholdDto(Household household);

    [MapProperty(nameof(Household.Users), nameof(HouseholdDto.UsersCount), Use = nameof(MapUserCount))]
    public static partial IEnumerable<HouseholdDto> ToHouseholdDtos(IEnumerable<Household> households);
    
    public static partial UpdateHouseholdDto ToUpdateDto(UpdateHouseholdRequest request, Guid userId);
    
    public static partial AddUserToHouseholdDto ToAddUserDto(AddUserToHouseholdRequest request, Guid inviterId);

    public static partial RemoveUserFromHouseholdDto ToRemoveUserDto(RemoveUserFromHouseholdRequest request, Guid removerId);
    
    private static int MapUserCount(ICollection<User> users) => users.Count;
}