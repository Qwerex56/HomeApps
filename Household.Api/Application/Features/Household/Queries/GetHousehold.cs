using FluentResults;
using HouseholdService.Application.Response;
using HouseholdService.Infrastructure.Repositories;
using MediatR;

namespace HouseholdService.Application.Features.Household.Queries;

public record GetHouseholdQuery(
    Guid UserId,
    Guid HouseholdId
) : IRequest<Result<HouseholdResponse>>;

public class GetHouseholdQueryHandler : IRequestHandler<GetHouseholdQuery, Result<HouseholdResponse>> {
    private readonly ILogger<GetHouseholdQueryHandler> _logger;
    private readonly UserHouseholdRepository _userHouseholdRepository;

    public GetHouseholdQueryHandler(ILogger<GetHouseholdQueryHandler> logger,
                                    UserHouseholdRepository userHouseholdRepository) {
        _logger = logger;
        _userHouseholdRepository = userHouseholdRepository;
    }

    public async Task<Result<HouseholdResponse>>
        Handle(GetHouseholdQuery request, CancellationToken cancellationToken) {
        var userHousehold =
            await _userHouseholdRepository.GetByHouseholdAndUserIdAsync(request.HouseholdId, request.UserId);

        if (userHousehold is null) {
            return Result.Fail("Household.NotFound");
        }

        var household = userHousehold.Household;

        return Result.Ok(new HouseholdResponse(
                             household.Id,
                             household.Name,
                             household.Description
                         ));
    }
}