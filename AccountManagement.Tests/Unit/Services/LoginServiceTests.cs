using System.IdentityModel.Tokens.Jwt;
using AccountManagement.Dto.LoginDto;
using AccountManagement.Models;
using AccountManagement.Repositories.JwtRepository;
using AccountManagement.Repositories.UserCredentialRepository;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Services.LoginService;
using AccountManagement.Workers.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AccountManagement.Tests.Unit.Services;

public class LoginServiceTests {
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock = new();
    
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUserCredentialRepository> _userCredentialRepositoryMock = new();
    private readonly Mock<IJwtRepository> _jwtRepositoryMock = new();

    
    private LoginService CreateService()
    {
        return new LoginService(
            _passwordHasherMock.Object,
            _userRepositoryMock.Object,
            _userCredentialRepositoryMock.Object,
            _jwtRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task RefreshUserSession_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var service = CreateService();

        var userId = Guid.NewGuid();
        const string rawToken = "raw";
        const string hashToken = "hash";

        var storedToken = new RefreshToken
        {
            UserId = userId,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddHours(12),
            TokenHash = hashToken
        };

        _jwtRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(storedToken);

        var newRefresh = new RefreshTokenDto
        {
            Token = "newRaw",
            Expires = DateTime.UtcNow.AddDays(60)
        };

        // UWAGA: to są metody LoginService, więc muszą być wirtualne albo w interfejsie
        var serviceMock = new Mock<LoginService>(_jwtRepositoryMock.Object, _unitOfWorkMock.Object)
        {
            CallBase = true
        };

        serviceMock
            .Setup(s => s.GenerateRefreshTokenAsync(userId))
            .ReturnsAsync(newRefresh);

        serviceMock
            .Setup(s => s.GenerateJwtTokenAsync(userId))
            .ReturnsAsync(new JwtSecurityToken());

        // Act
        var result = await serviceMock.Object.RefreshUserSession(rawToken);

        // Assert
        Assert.Equal(newRefresh.Token, result.RefreshToken.Token);
        _jwtRepositoryMock.Verify(r => r.DeleteAsync(storedToken), Times.Once);
    }
}