using AccountManagement.Services.LoginService;
using AccountManagement.Workers.UnitOfWork;
using Moq;

namespace AccountManagement.Tests.Unit.Services;

public class LoginServiceTests {
    
    private readonly Mock<ILoginService> _loginServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    [Fact]
    public async Task RefreshUserSession_ShouldReturnNewTokens_WhenFreshTokenIsValid() {
        const string rawToken = "raw";
        const string hashToken = "hash";
        
    }
}