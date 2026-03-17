using Household.Api.Data;
using Household.Api.Extensions;
using Household.Api.Repositories;
using MassTransit;
using Shared.Contracts.Messages;
using Shared.Data;

namespace Household.Api.EventConsumers.User;

public class UserCreatedConsumer : IConsumer<UserCreated> {
    private readonly UserRepository _userRepository;
    private readonly UnitOfWork _uow;

    public UserCreatedConsumer(UserRepository userRepository, UnitOfWork uow) {
        _userRepository = userRepository;
        _uow = uow;
    }

    public async Task Consume(ConsumeContext<UserCreated> context) {
        var userExists = await _userRepository.GetByIdAsync(context.Message.UserId);

        if (userExists is not null) {
            return;
        }

        var user = new Models.User {
            Id = context.Message.UserId,
            Username = context.Message.Username,
            Email = context.Message.Email,
            Role = context.Message.Role,
        };
        
        await _userRepository.AddAsync(user);
        await _uow.SaveChangesAsync();
    }
}