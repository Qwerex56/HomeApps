using AccountManagement.Dto.User;
using AccountManagement.Models;
using AccountManagement.Repositories.ExternalCredentialRepository;
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
    private readonly IExternalCredentialRepository _externalCredentialRepository;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork, IPasswordHasher<User> passwordHasher,
        IUserCredentialRepository userCredentialRepository,
        IExternalCredentialRepository externalCredentialRepository) {
        _userRepository = userRepository;
        _userCredentialRepository = userCredentialRepository;
        _externalCredentialRepository = externalCredentialRepository;

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
            Role = createUserByAdminDto.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var passwordHash = _passwordHasher.HashPassword(user, createUserByAdminDto.Password);

        var userCredentials = new UserCredential {
            UserId = user.Id,
            Email = email,
            PasswordHash = passwordHash,
        };
        
        user.UserCredential = userCredentials;

        await _userRepository.CreateAsync(user);
        await _userCredentialRepository.CreateAsync(userCredentials);
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var userCredentials = new ExternalCredentials {
            UserId = user.Id,
            ProviderId = createUserExternalDto.ProviderId,
            ProviderName = createUserExternalDto.ProviderName,

            CreatedAt = DateTime.UtcNow,
        };

        await _userRepository.CreateAsync(user);
        await _externalCredentialRepository.CreateAsync(userCredentials);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<bool> AccountExistsByEmailAsync(string email) {
        var credential = await _userCredentialRepository.GetByEmailAsync(email);

        return credential is not null;
    }

    public async Task<bool> AccountExistsByProviderId(string providerId) {
        var credential = await _externalCredentialRepository.GetExternalCredentialByProviderId(providerId);

        return credential is not null;
    }

    public async Task<User?> GetUserByEmailAsync(string email) {
        var credential = await _userCredentialRepository.GetByEmailAsync(email);

        if (credential is null) {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(credential.UserId);
        return user;
    }

    public Task<User?> GetUserByProviderIdAsync(string providerId) {
        throw new NotImplementedException();
    }

    public async Task UpdateUserInfo(UserSelfUpdateDto userSelfUpdateDto, string userId) {
        var user = await GetUserByIdAsync(Guid.Parse(userId));

        if (user is null) {
            throw new UserNotFoundException(userId);
        }

        if (userSelfUpdateDto.UserName is not null) {
            NameValidator.Validate(userSelfUpdateDto.UserName);

            user.Name = userSelfUpdateDto.UserName;
        }

        if (userSelfUpdateDto.Email is not null) {
            EmailValidator.Validate(userSelfUpdateDto.Email);

            if (await _userCredentialRepository.GetByEmailAsync(userSelfUpdateDto.Email) is null) {
                throw new EmailDuplicationException(userSelfUpdateDto.Email);
            }

            if (user.UserCredential != null) 
                user.UserCredential.Email = userSelfUpdateDto.Email;
        }

        if (userSelfUpdateDto.Password is not null) {
            PasswordValidator.Validate(userSelfUpdateDto.Password);

            var passwordHash = _passwordHasher.HashPassword(user, userSelfUpdateDto.Password);
            if (user.UserCredential != null) 
                user.UserCredential.PasswordHash = passwordHash;
        }

        await _unitOfWork.SaveChangesAsync();
    }

    // Sets account for deactivation, deactivated user will be still valid for X days
    public async Task UserSoftDeleteById(string userId) {
        var user = await GetUserByIdAsync(Guid.Parse(userId));

        if (user is null) {
            throw new UserNotFoundException(userId);
        }

        user.IsActive = false;
        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
}