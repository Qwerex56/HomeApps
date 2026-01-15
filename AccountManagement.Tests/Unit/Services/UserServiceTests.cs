using AccountManagement.Dto.User;
using AccountManagement.Models;
using AccountManagement.Repositories.UserRepository;
using AccountManagement.Services.UserService;
using AccountManagement.Workers.UnitOfWork;
using FluentAssertions;
using Moq;

namespace AccountManagement.Tests.Unit.Services;

public class UserServiceTests {
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly UserService _userService;

    public UserServiceTests() {
        _userService = new UserService(_userRepositoryMock.Object,  _unitOfWorkMock.Object);
    }
}