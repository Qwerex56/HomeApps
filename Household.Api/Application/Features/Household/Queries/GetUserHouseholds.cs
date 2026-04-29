using FluentResults;
using HouseholdService.Application.Response;
using HouseholdService.Infrastructure.Repositories;
using MediatR;

namespace HouseholdService.Application.Features.Household.Queries;

public record GetUserHouseholdsQuery(
    Guid UserId,
    int Page = 0,
    int PageSize = 20
) : IRequest<Result<HouseholdListResponse>>;

public class
    GetUserHouseholdsQueryHandler : IRequestHandler<GetUserHouseholdsQuery, Result<HouseholdListResponse>> {
    private readonly ILogger<GetUserHouseholdsQueryHandler> _logger;

    private readonly UserHouseholdRepository _userHouseholdRepository;
    private readonly UserRepository _userRepository;

    public GetUserHouseholdsQueryHandler(ILogger<GetUserHouseholdsQueryHandler> logger,
                                         UserHouseholdRepository userHouseholdRepository,
                                         UserRepository userRepository) {
        _logger = logger;
        _userHouseholdRepository = userHouseholdRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<HouseholdListResponse>> Handle(GetUserHouseholdsQuery request,
                                                            CancellationToken cancellationToken) {
        var userExists = await _userRepository.UserExist(request.UserId, cancellationToken);

        if (!userExists) {
            return Result.Fail("User.NotFound");
        }


        var userList =
            await _userHouseholdRepository.GetByUserIdAsync(request.UserId, request.PageSize, request.Page,
                                                            cancellationToken);
        var totalItems = await _userHouseholdRepository.GetItemCountByUserIdAsync(request.UserId, cancellationToken);

        return Result.Ok(
            new HouseholdListResponse(
                userList.Select(uh => uh.Household).ToResponse(),
                request.PageSize,
                request.Page,
                totalItems
            )
        );
    }
}