using FluentResults;
using HouseholdService.Application.Response;
using HouseholdService.Infrastructure.Repositories;
using MediatR;

namespace HouseholdService.Application.Features.Household.Queries;

public record GetHouseholdUserListQuery(
    Guid HouseholdId,
    Guid UserId,
    int PageSize = 20,
    int Page = 0
) : IRequest<Result<HouseholdUserListResponse>>;

public class
    GetHouseholdUserListQueryHandler : IRequestHandler<GetHouseholdUserListQuery, Result<HouseholdUserListResponse>> {
    private readonly ILogger<GetHouseholdUserListQueryHandler> _logger;

    private readonly HouseholdRepository _householdRepository;

    public GetHouseholdUserListQueryHandler(ILogger<GetHouseholdUserListQueryHandler> logger,
                                            HouseholdRepository householdRepository) {
        _logger = logger;
        _householdRepository = householdRepository;
    }

    public async Task<Result<HouseholdUserListResponse>> Handle(GetHouseholdUserListQuery request,
                                                                CancellationToken cancellationToken) {
        var household = await _householdRepository.GetByIdAsync(request.HouseholdId, cancellationToken);

        if (household is null) {
            return Result.Fail("Household.NotFound");
        }

        if (!household.HasUser(request.UserId)) {
            return Result.Fail("User.NotFound");
        }

        var userList =
            await _householdRepository.GetUserListAsync(request.HouseholdId,
                                                        page: request.Page,
                                                        pageSize: request.PageSize,
                                                        cancellationToken: cancellationToken);

        return Result.Ok(new HouseholdUserListResponse(
                             Household: household.ToHouseholdResponse(),
                             UserHouseholds: userList.ToHouseholdUserList()
                         ));
    }
}