using FluentResults;
using Household.Api.Extensions;
using Household.Api.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Authorization;

namespace Household.Api.Services.Household.Commands.CreateHousehold;

public class CreateHouseholdHandler : IRequestHandler<CreateHouseholdCommand, Result> {
    private readonly HouseholdRepository _householdRepository;
    private readonly UserRepository _userRepository;

    private readonly UnitOfWork _unitOfWork;
    private readonly ILogger<CreateHouseholdHandler> _logger;

    public CreateHouseholdHandler(HouseholdRepository householdRepository, UserRepository userRepository,
        UnitOfWork unitOfWork, ILogger<CreateHouseholdHandler> logger) {
        _householdRepository = householdRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CreateHouseholdCommand request, CancellationToken cancellationToken) {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user is null) {
            return Result.Fail("User.NotFound");
        }

        var household = Models.Household.Create(
            ownerId: user.Id,
            name: request.Name,
            description: request.Description
        );

        await _unitOfWork.HouseholdRepository.AddAsync(household);

        try {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException ex) {
            _logger.LogError(ex.Message);
            _logger.LogError("Could not save household: {HouseholdId}, to the database. ", household.Id);
            return Result.Fail("Household.SaveFailed");
        }

        return Result.Ok();
    }
}