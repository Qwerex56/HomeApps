using Household.Api.Data;
using Household.Api.Extensions;
using Household.Api.Repositories;
using Household.Api.Services.Household.Commands.AddMember;
using MediatR;
using Shared.Data;

namespace Household.Api.Services.Invite.Commands.AcceptInvite;

public class AcceptInviteHandler : IRequestHandler<AcceptInviteCommand> {
    private readonly InviteRepository _inviteRepository;
    private readonly IMediator _mediator;
    private readonly UnitOfWork _unitOfWork;

    public AcceptInviteHandler(InviteRepository inviteRepository, IMediator mediator,
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

        await _mediator.Send(new AddMemberCommand(invite.HouseholdId, request.UserId), cancellationToken);

        invite.UsedBy.Add(request.UserId);
        await _unitOfWork.SaveChangesAsync();
    }
}