using HouseholdService.Infrastructure.Extensions;
using HouseholdService.Infrastructure.Repositories;
using MassTransit;
using Shared.Contracts.Messages;

namespace HouseholdService.Infrastructure.EventConsumers.User;

public class UserCreatedConsumer : IConsumer<UserCreated> {
    private readonly UserRepository _userRepository;
    private readonly UnitOfWork _uow;

    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(UserRepository userRepository, UnitOfWork uow, ILogger<UserCreatedConsumer> logger) {
        _userRepository = userRepository;
        _uow = uow;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreated> context) {
        var userExists = await _userRepository.GetByIdAsync(context.Message.UserId);

        if (userExists is not null) {
            return;
        }

        var user = new HouseholdService.Domain.Models.User {
            Id = context.Message.UserId,
            Username = context.Message.Username,
            Email = context.Message.Email,
            Role = context.Message.Role,
        };

        await _userRepository.AddAsync(user);
        await _uow.SaveChangesAsync();
        
        _logger.LogInformation($"User {context.Message.UserId} created {context.Message.Username}");
    }
}