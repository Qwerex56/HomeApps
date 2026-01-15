using AccountManagement.Dto.User;
using AccountManagement.Hashers.PasswordHasher;
using AccountManagement.Models;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Workers.UnitOfWork;
using Shared.Exceptions.Service;
using Shared.Validators;

namespace AccountManagement.Services.UserService;

public class UserService : IUserService {
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork) {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> GetUserByEmailAsync(string email) {
        var user = await _userRepository.GetByEmailAsync(email);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(Guid id) {
        var user = await _userRepository.GetByIdAsync(id);
        return user;
    }

    public async Task<User> CreateUserWithPasswordAsync(CreateUserByAdminDto createUserByAdminDto) {
        var email = createUserByAdminDto.Email.Trim().ToLowerInvariant();
        var name = createUserByAdminDto.Name.Trim();

        NameValidator.Validate(name);
        EmailValidator.Validate(email);
        PasswordValidator.Validate(createUserByAdminDto.Password);

        var userExists = await _userRepository.GetByEmailAsync(email);

        if (userExists is not null) {
            throw new EmailDuplicationException(createUserByAdminDto.Email);
        }

        var user = new User {
            Email = email,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        
        var (passwordHash, saltHash) = _passwordHasher.HashPassword(createUserByAdminDto.Password);
        
        var userCredentials = new UserCredentials {
            UserId = user.Id,
            User = user,
            PasswordHash = passwordHash,
            PasswordSalt = saltHash,
        };

        await _userRepository.CreateAsync(user);
        await _userRepository.CreateUserCredentialsAsync(userCredentials);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<User> CreateExternalUserAsync(CreateUserExternalDto createUserExternalDto) {
        var email = createUserExternalDto.Email.Trim().ToLowerInvariant();
        var name = createUserExternalDto.Name.Trim();

        NameValidator.Validate(name);
        EmailValidator.Validate(email);

        var userExists = await _userRepository.GetByEmailAsync(email);

        if (userExists is not null) {
            throw new EmailDuplicationException(createUserExternalDto.Email);
        }

        var user = new User {
            Email = email,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        // TODO: Create External credentials entity

        await _userRepository.CreateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }
}