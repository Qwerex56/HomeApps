using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AccountManagement.Dto.Credentials;
using AccountManagement.Dto.LoginDto;
using AccountManagement.Models;
using AccountManagement.Repositories.JwtRepository;
using AccountManagement.Repositories.UserCredentialRepository;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Workers.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Shared.Exceptions.Service;

namespace AccountManagement.Services.LoginService;

public class LoginService : ILoginService {
    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly IUserRepository _userRepository;
    private readonly IUserCredentialRepository _userCredentialRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    private readonly IUnitOfWork _unitOfWork;

    public LoginService(IPasswordHasher<User> passwordHasher, IUserRepository userRepository,
        IUserCredentialRepository userCredentialRepository, IRefreshTokenRepository refreshTokenRepository, IUnitOfWork unitOfWork) {
        _passwordHasher = passwordHasher;
        _userRepository = userRepository;
        _userCredentialRepository = userCredentialRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User?> ValidateCredentials(UserCredentialsDto providedCredentialsDto) {
        var credential = await _userCredentialRepository.GetByEmailAsync(providedCredentialsDto.Email);

        if (credential is null) {
            throw new UserNotFoundException(providedCredentialsDto.Email);
        }

        var user = await _userRepository.GetByIdAsync(credential.UserId);

        if (user is null) {
            throw new UserNotFoundException(providedCredentialsDto.Email);
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(user, credential.PasswordHash, providedCredentialsDto.Password);

        if (verificationResult == PasswordVerificationResult.Failed) {
            throw new InvalidUserCredentialException(providedCredentialsDto.Email + ' ' +
                                                     providedCredentialsDto.Password);
        }

        return user;
    }

    public async Task<JwtSecurityToken> GenerateJwtTokenAsync(Guid userId) {
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null) {
            throw new UserNotFoundException(userId.ToString());
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SECURITY_KEY"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims = [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        ];

        var token = new JwtSecurityToken(
            issuer: "ISSUER",
            audience: "AUDIENCE",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);

        return token;
    }

    public async Task<RefreshTokenDto> GenerateRefreshTokenAsync(Guid userId) {
        var (refreshRaw, refreshHash) = GenerateRefreshTokens();

        var token = new RefreshToken {
            UserId = userId,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(60),

            TokenHash = refreshHash
        };

        await _refreshTokenRepository.CreateAsync(token);
        await _unitOfWork.SaveChangesAsync();

        return new RefreshTokenDto {
            Token = refreshRaw,
            Expires = token.Expires
        };
    }

    public async Task<RefreshTokenWithJwtDto> RefreshUserSession(string token) {
        var tokenHash = HashRefreshToken(token);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (refreshToken is null) {
            throw new TokenNotFoundException();
        }

        if (refreshToken.Expires < DateTime.UtcNow) {
            throw new TokenExpiredException();
        }
        
        await _refreshTokenRepository.DeleteAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();
        
        var newRefresh = await GenerateRefreshTokenAsync(refreshToken.UserId);
        var newJwt = await GenerateJwtTokenAsync(refreshToken.UserId);

        return new RefreshTokenWithJwtDto {
            RefreshToken = newRefresh,
            JwtToken = newJwt
        };
    }

    public async Task RemoveRefreshTokenWithHash(string token) {
        var tokenHash = HashRefreshToken(token);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (refreshToken is null) {
            throw new TokenNotFoundException(token);
        }

        await _refreshTokenRepository.DeleteAsync(refreshToken);
        await _unitOfWork.SaveChangesAsync();
    }

    private static (string raw, string hashed) GenerateRefreshTokens() {
        var refreshBytes = RandomNumberGenerator.GetBytes(64);
        var refreshRaw = Convert.ToBase64String(refreshBytes);

        var refreshHashBytes = SHA256.HashData(refreshBytes);
        var refreshHash = Convert.ToBase64String(refreshHashBytes);

        return (refreshRaw, refreshHash);
    }

    private static string HashRefreshToken(string refreshToken) {
        var refreshBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = SHA256.HashData(refreshBytes);

        return Convert.ToBase64String(hashBytes);
    }
}