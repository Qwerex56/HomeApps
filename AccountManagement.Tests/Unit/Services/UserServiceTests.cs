using AccountManagement.Dto.User;
using AccountManagement.Models;
using AccountManagement.Repositories.ExternalCredentialRepository;
using AccountManagement.Repositories.UserCredentialRepository;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Services.UserService;
using AccountManagement.Workers.UnitOfWork;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Shared.Authorization;
using Shared.Exceptions.Service;
using Shared.Exceptions.Validators;

namespace AccountManagement.Tests.Unit.Services;

public class UserServiceTests {
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IUserCredentialRepository> _userCredRepoMock = new();
    private readonly Mock<IExternalCredentialRepository> _extCredRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly PasswordHasher<User> _passwordHasher = new();

    private readonly UserService _userService;

    public UserServiceTests() {
        _userService = new UserService(
            _userRepoMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasher,
            _userCredRepoMock.Object,
            _extCredRepoMock.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenGuidIsValid() {
        var mockId = Guid.NewGuid();
        var mockDataUser = new User() {
            Id = mockId,
        };

        _userRepoMock
            .Setup(s => s.GetByIdAsync(mockId))
            .ReturnsAsync(mockDataUser);

        var result = await _userService.GetUserByIdAsync(mockDataUser.Id);

        _userRepoMock.Verify(s => s.GetByIdAsync(mockId), Times.Once);

        Assert.NotNull(result);
        Assert.Equal(mockDataUser.Id, result.Id);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenGuidIsInvalid() {
        var mockId = Guid.NewGuid();

        _userRepoMock
            .Setup(s => s.GetByIdAsync(mockId))
            .ReturnsAsync((User?)null);

        var result = await _userService.GetUserByIdAsync(mockId);

        _userRepoMock.Verify(s => s.GetByIdAsync(mockId), Times.Once);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateUserWithPasswordAsync_ShouldCreateUser_WhenDtoIsValid() {
        var mockDto = new CreateUserByAdminDto {
            Email = "valid@email.com",
            Name = "ValidName",
            Password = "Val1dP@ssword",
            Role = UserSystemRoleEnum.SystemMember
        };

        _userCredRepoMock
            .Setup(s => s.GetByEmailAsync(mockDto.Email))
            .ReturnsAsync((UserCredential?)null);

        _userRepoMock
            .Setup(s => s.CreateAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _userCredRepoMock
            .Setup(s => s.CreateAsync(It.IsAny<UserCredential>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(s => s.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var result = await _userService.CreateUserWithPasswordAsync(mockDto);

        _userCredRepoMock.Verify(s => s.GetByEmailAsync(mockDto.Email), Times.Once);
        _userRepoMock.Verify(s => s.CreateAsync(It.IsAny<User>()), Times.Once);
        _userCredRepoMock.Verify(s => s.CreateAsync(It.IsAny<UserCredential>()), Times.Once);
        _unitOfWorkMock.Verify(s => s.SaveChangesAsync(), Times.Once);

        Assert.NotNull(result);
        Assert.NotNull(result.UserCredential);

        Assert.Equal(result.Id, result.UserCredential.UserId);
        Assert.Equal(mockDto.Name, result.Name);
        Assert.Equal(mockDto.Email, result.UserCredential.Email);
        Assert.Equal(mockDto.Role, result.Role);

        var verificationResult = _passwordHasher.VerifyHashedPassword(result,
            result.UserCredential.PasswordHash,
            mockDto.Password);

        Assert.Equal(PasswordVerificationResult.Success, verificationResult);
    }

    [Fact]
    public async Task CreateUserWithPasswordAsync_ShouldThrowException_WhenUserExists() {
        var mockDto = new CreateUserByAdminDto {
            Email = "valid@email.com",
            Name = "ValidName",
            Password = "Val1dP@ssword",
            Role = UserSystemRoleEnum.SystemMember
        };

        var existingUser = new User {
            Id = Guid.NewGuid(),
            Name = mockDto.Name,
            Role = mockDto.Role
        };

        var existingUserCredential = new UserCredential {
            Id = Guid.NewGuid(),
            UserId = existingUser.Id,
            Email = mockDto.Email,
            PasswordHash = _passwordHasher.HashPassword(existingUser, mockDto.Password)
        };
        
        existingUser.UserCredential = existingUserCredential;
        
        _userCredRepoMock
            .Setup(s => s.GetByEmailAsync(mockDto.Email))
            .ReturnsAsync(existingUserCredential);

        await Assert.ThrowsAsync<EmailDuplicationException>(async () => {
            await _userService.CreateUserWithPasswordAsync(mockDto);
        });
        
        _userCredRepoMock.Verify(s => s.GetByEmailAsync(mockDto.Email), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invlidEmail")]
    [InlineData("Invlaid email")]
    [InlineData("Invlaidemail@example")]
    public async Task CreateUserWithPasswordAsync_ShouldThrowException_WhenEmailIsInvalidOrNull(string email) {
        var mockDto = new CreateUserByAdminDto {
            Email = email,
            Name = "ValidName",
            Password = "Val1dP@ssword",
            Role = UserSystemRoleEnum.SystemMember
        };

        await Assert.ThrowsAsync<EmailFormatException>(async () => {
            await _userService.CreateUserWithPasswordAsync(mockDto);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("na")]
    [InlineData("Na  ")]
    public async Task CreateUserWithPasswordAsync_ShouldThrowException_WhenNameIsTooShortOrNull(string name) {
        var mockDto = new CreateUserByAdminDto {
            Email = "valid@email.example",
            Name = name,
            Password = "Val1dP@ssword",
            Role = UserSystemRoleEnum.SystemMember
        };

        await Assert.ThrowsAsync<NameTooShortException>(async () => {
            await _userService.CreateUserWithPasswordAsync(mockDto);
        });
    }

    [Theory]
    [InlineData("123Name")]
    [InlineData("123")]
    [InlineData("Adri@n")]
    [InlineData("1User")]
    [InlineData("_user")]
    [InlineData("User Name")]
    [InlineData("User-Name")]
    public async Task CreateUserWithPasswordAsync_ShouldThrowException_WhenNameIsInvalid(string name) {
        var mockDto = new CreateUserByAdminDto {
            Email = "valid@email.example",
            Name = name,
            Password = "Val1dP@ssword",
            Role = UserSystemRoleEnum.SystemMember
        };

        await Assert.ThrowsAsync<NameFormatException>(async () => {
            await _userService.CreateUserWithPasswordAsync(mockDto);
        });
    }

    [Fact]
    public async Task AccountExistsByEmailAsync_ShouldReturnTrue_WhenCredentialIsValid() {
        // Arrange
        _userCredRepoMock
            .Setup(s => s.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new UserCredential());

        // Act
        var result = await _userService.AccountExistsByEmailAsync("email@example.com");

        // Assert
        _userCredRepoMock.Verify(s => s.GetByEmailAsync("email@example.com"), Times.Once);
        Assert.True(result);
    }

    [Fact]
    public async Task AccountExistsByEmailAsync_ShouldReturnFalse_WhenCredentialIsNull() {
        _userCredRepoMock
            .Setup(s => s.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((UserCredential?)null);

        var result = await _userService.AccountExistsByEmailAsync("email@example.com");

        _userCredRepoMock.Verify(s => s.GetByEmailAsync("email@example.com"), Times.Once);
        Assert.False(result);
    }
}