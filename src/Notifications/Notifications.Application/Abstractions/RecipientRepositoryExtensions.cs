using Notifications.Domain.Recipients;

namespace Notifications.Application.Abstractions;

internal static class RecipientRepositoryExtensions
{
    /// <summary>The person's recipient, created on their first device or setting.</summary>
    public static async Task<Recipient> GetOrCreateAsync(this IRecipientRepository recipients, Guid userId, CancellationToken cancellationToken)
    {
        var recipient = await recipients.FindAsync(userId, cancellationToken);
        if (recipient is not null)
        {
            return recipient;
        }

        recipient = Recipient.Create(userId);
        recipients.Add(recipient);
        return recipient;
    }
}
