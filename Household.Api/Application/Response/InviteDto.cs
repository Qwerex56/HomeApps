using HouseholdService.Domain.Models;

namespace HouseholdService.Application.Response;

public record InviteDto(
    string InviteCode,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    Guid CreatedBy,
    Guid HouseholdId,
    ICollection<Guid> UsedBy
);

public static class InviteMapper {
    public static InviteDto ToInviteDto(Invite invite) {
        return new InviteDto(
            invite.InviteCode,
            invite.CreatedAt,
            invite.ExpiresAt,
            invite.RevokedAt,
            invite.CreatedBy,
            invite.HouseholdId,
            invite.UsedBy
        );
    }

    public static IEnumerable<InviteDto> ToInviteDtos(IEnumerable<Invite> invites) => invites.Select(ToInviteDto);
}