using HouseholdService.Application.Features.Household.Commands.AddMember;
using HouseholdService.Infrastructure.Extensions;
using HouseholdService.Infrastructure.Repositories;
using MediatR;

namespace HouseholdService.Application.Features.Invite.Commands.AcceptInvite;

public class AcceptInviteHandler : IRequestHandler<AcceptInviteCommand> {
    private readonly InviteRepository _inviteRepository;
    private readonly IMediator _mediator;
    private readonly UnitOfWork _unitOfWork;

    public AcceptInviteHandler(InviteRepository inviteRepository,
                               IMediator mediator,
                               UnitOfWork unitOfWork) {
        _inviteRepository = inviteRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AcceptInviteCommand request, CancellationToken cancellationToken) {
        var invite = await _inviteRepository.GetByCodeAsync(request.InviteCode);

        if (invite is null || !invite.IsActive) {
            throw new InvalidOperationException("Invite does not exist");
        }

        await _mediator.Send(new AddMemberCommand(invite.HouseholdId, invite.CreatedBy, request.UserId),
                             cancellationToken);

        invite.UsedBy.Add(request.UserId);
        await _unitOfWork.SaveChangesAsync();
    }
}