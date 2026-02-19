using AccountManagement.Dto.User;
using AccountManagement.Models;

namespace AccountManagement.Services.UserService;

public interface IUserService {
    public Task<User?> GetUserByIdAsync(Guid id);
    
    public Task<User> CreateUserWithPasswordAsync(CreateUserByAdminDto createUserByAdminDto);

    public Task<User> CreateExternalUserAsync(CreateUserExternalDto createUserExternalDto);
    
    public Task<bool> AccountExistsByEmailAsync(string email);
    
    public Task<bool> AccountExistsByProviderId(string providerId);
    
    public Task<User?> GetUserByEmailAsync(string email);
    
    public Task<User?> GetUserByProviderIdAsync(string providerId);
    public Task UpdateUserInfo(UserSelfUpdateDto userSelfUpdateDto, string userId);
    public Task UserSoftDeleteById(string userId);
}