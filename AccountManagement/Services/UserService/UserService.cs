using AccountManagement.Dto.User;
using AccountManagement.Models;
using AccountManagement.Repositories.UserCredentialRepository;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Workers.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Shared.Exceptions.Service;
using Shared.Validators;

namespace AccountManagement.Services.UserService;

public class UserService : IUserService {
    private readonly IUserRepository _userRepository;
    private readonly IUserCredentialRepository _userCredentialRepository;
    
    private readonly IUnitOfWork _unitOfWork;
    
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher, IUserCredentialRepository userCredentialRepository) {
        _userRepository = userRepository;
        _userCredentialRepository = userCredentialRepository;
        
        _unitOfWork = unitOfWork;
        
        _passwordHasher = passwordHasher;
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

        var userExists = await _userCredentialRepository.GetByEmailAsync(email);

        if (userExists is not null) {
            throw new EmailDuplicationException(createUserByAdminDto.Email);
        }

        var user = new User {
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt =  DateTime.UtcNow
        };
        
        var passwordHash = _passwordHasher.HashPassword(user, createUserByAdminDto.Password);
        
        var userCredentials = new UserCredential {
            UserId = user.Id,
            User = user,
            Email = email,
            PasswordHash = passwordHash,
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

        var userExists = await _userCredentialRepository.GetByEmailAsync(email);

        if (userExists is not null) {
            throw new EmailDuplicationException(createUserExternalDto.Email);
        }

        var user = new User {
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        // TODO: Create External credentials entity

        await _userRepository.CreateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }
}