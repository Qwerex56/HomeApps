using Household.Api.Data;
using Household.Api.Extensions;
using Household.Api.Models;
using Household.Api.Repositories;
using MediatR;
using Shared.Authorization;
using Shared.Data;

namespace Household.Api.Services.Household.Commands.AddMember;

public class AddMemberHandler : IRequestHandler<AddMemberCommand> {
    private readonly HouseholdRepository _householdRepository;
    private readonly UserRepository _userRepository;
    private readonly IMediator _mediator;
    private readonly UnitOfWork _unitOfWork;

    public AddMemberHandler(HouseholdRepository householdRepository, IMediator mediator,
        UnitOfWork unitOfWork, UserRepository userRepository) {
        _householdRepository = householdRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task Handle(AddMemberCommand request, CancellationToken cancellationToken) {
        var household = await _householdRepository.GetByIdAsync(request.HouseholdId);
        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (household is null) {
            throw new InvalidOperationException("Household does not exist");
        }

        if (user is null || household.Users.Any(u => u.Id == request.UserId)) {
            throw new InvalidOperationException("User does not exist");
        }

        var userHousehold = new UserHousehold {
            HouseholdId = request.HouseholdId,
            UserId = request.UserId,
            Nickname = user.Username,
            UserFamilyRole = UserFamilyRoleEnum.FamilyMember
        };

        await _unitOfWork.UserHouseholdRepository.AddAsync(userHousehold);
        await _unitOfWork.SaveChangesAsync();
    }
}