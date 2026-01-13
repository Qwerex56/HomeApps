using AccountManagement.Dto.User;
using AccountManagement.Models;
using Riok.Mapperly.Abstractions;

namespace AccountManagement.Mappers;

[Mapper]
public static partial class UserMapper {
    public static partial CreatedUserDto ToCreatedUserDto(User user);
    
}