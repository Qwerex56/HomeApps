using AccountService.Application.Dto.User;
using AccountService.Domain.Models;
using Riok.Mapperly.Abstractions;

namespace AccountService.Infrastructure.Mappers;

[Mapper]
public static partial class UserMapper {
    public static partial CreatedUserDto ToCreatedUserDto(User user);
    
    public static partial GetUserDto ToGetUserDto(User user);
}