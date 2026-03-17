using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Household.Api.Models;

[Index(nameof(InviteCode), IsUnique = true)]
public class Invite : Entity {
    [NotMapped]
    private const uint InviteExpirationDays = 30;
    
    [MaxLength(32)]
    public required string InviteCode { get; set; } = string.Empty;
    public required DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public required DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(InviteExpirationDays);
    public DateTime? RevokedAt { get; set; }
    
    public required Guid CreatedBy { get; set; }
    public required Guid HouseholdId { get; set; }

    public ICollection<Guid> UsedBy { get; init; } = [];
    
    [NotMapped]
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
    
    public static Invite Create(Guid requestHouseholdId, Guid requestInviterId) {
        return new Invite {
            InviteCode = GenerateInviteCode(requestHouseholdId, requestInviterId),
            CreatedBy = requestInviterId,
            HouseholdId = requestHouseholdId,
            
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(InviteExpirationDays),
        };
    }
    
    private static string GenerateInviteCode(Guid householdId, Guid inviterId) {
        var invite = householdId.ToString()[^4..] + '-' + inviterId.ToString()[^4..];
        return invite;
    }
}