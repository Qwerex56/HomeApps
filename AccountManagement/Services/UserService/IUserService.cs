using AccountManagement.Dto.User;
using AccountManagement.Models;

namespace AccountManagement.Services.UserService;

public interface IUserService {
    public Task<User?> GetUserByEmailAsync(string email);
    
    public Task<User?> GetUserByIdAsync(Guid id);
    
    public Task<User> CreateUserWithPasswordAsync(CreateUserByAdminDto createUserByAdminDto);

    public Task<User> CreateExternalUserAsync(CreateUserExternalDto createUserExternalDto);
}