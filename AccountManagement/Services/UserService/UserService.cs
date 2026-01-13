using AccountManagement.Dto.User;
using AccountManagement.Models;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Workers.UnitOfWork;
using Shared.Exceptions.Service;
using Shared.Validators;

namespace AccountManagement.Services.UserService;

public class UserService : IUserService {
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork) {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<User?> GetUserByEmailAsync(string email) {
        var user =  await _userRepository.GetByEmailAsync(email);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid id) {
        var user = await _userRepository.GetByIdAsync(id);
        return user;
    }
    
    public async Task<User> CreateUserAsync(CreateUserDto createUserDto) {
        var email = createUserDto.Email.Trim().ToLowerInvariant();
        var name  = createUserDto.Name.Trim().ToLowerInvariant();
        
        NameValidator.Validate(email);
        EmailValidator.Validate(name);
        
        var userExists = await _userRepository.GetByEmailAsync(email);

        if (userExists is not null) {
            throw new EmailDuplicationException(createUserDto.Email);
        }

        var user = new User {
            Email = email,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        
        await _userRepository.CreateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        
        return user;
    }
}