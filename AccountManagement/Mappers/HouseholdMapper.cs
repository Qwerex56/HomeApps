using AccountManagement.Dto.HouseholdDto;
using AccountManagement.Models;
using Riok.Mapperly.Abstractions;

namespace AccountManagement.Mappers;

[Mapper]
public static partial class HouseholdMapper {
    [MapperIgnoreSource(nameof(Household.UserHouseholds))]
    [MapProperty(nameof(Household.Users), nameof(HouseholdDto.UsersCount), Use = nameof(MapUserCount))]
    public static partial HouseholdDto ToHouseholdDto(Household household);

    [MapperIgnoreSource(nameof(Household.Users))]
    [MapProperty(nameof(Household.Users), nameof(HouseholdDto.UsersCount), Use = nameof(MapUserCount))]
    public static partial IEnumerable<HouseholdDto> ToHouseholdDtos(IEnumerable<Household> households);
    
    private static int MapUserCount(ICollection<User> users) => users.Count;
}