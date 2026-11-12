using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using HomeApps.Contracts.Notifications;
using HomeMix.Application.Abstractions;
using HomeMix.Application.Blocks;
using HomeMix.Domain.Data;
using HomeMix.Domain.Installations;
using HomeMix.Domain.Manifests;

namespace HomeMix.Application.Notify.PushNotification;

/// <summary>
/// Anyone who uses the installation may send (the add-on decides when); recipients must be in its household. The
/// source people mute by is the add-on id, the category is <see cref="Category"/>.
/// </summary>
internal sealed class PushNotificationHandler(
    IInstallationRepository installations,
    IHouseholdRoles households,
    Blocklist blocklist,
    ICurrentUser currentUser,
    IEventBus bus) : ICommandHandler<PushNotificationCommand, PushNotificationResult>
{
    public const string Category = "addon";

    public async Task<Result<PushNotificationResult>> Handle(PushNotificationCommand command, CancellationToken cancellationToken)
    {
        var installation = await installations.FindAsync(command.InstallationId, cancellationToken);
        if (installation is null || !installation.IsActive
            || await blocklist.IsBlockedAsync(installation.AddonId, installation.PackageSha256, cancellationToken))
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        var me = currentUser.RequiredUserId;
        IReadOnlyList<Guid> everyone;
        if (installation.TargetKind == TargetKind.Household)
        {
            if (await households.GetRoleAsync(installation.TargetId, cancellationToken) is null)
            {
                return Result.Fail(InstallationErrors.NotFound);
            }

            everyone = await households.GetMemberIdsAsync(installation.TargetId, cancellationToken);
        }
        else if (installation.TargetId == me)
        {
            everyone = [me];
        }
        else
        {
            return Result.Fail(InstallationErrors.NotFound);
        }

        if (!installation.Capabilities.Contains(AddonCapabilities.Notify))
        {
            return Result.Fail(DataErrors.CapabilityMissing(AddonCapabilities.Notify));
        }

        var recipients = command.UserIds is null
            ? everyone.Where(id => id != me || installation.TargetKind == TargetKind.User).ToList()
            : command.UserIds.Distinct().ToList();
        if (recipients.Any(id => !everyone.Contains(id)))
        {
            return Result.Fail(InstallationErrors.NotARecipient);
        }

        if (recipients.Count > 0)
        {
            var page = installation.TargetKind == TargetKind.Household
                ? $"/homes/{installation.TargetId}/mix/{installation.AddonId}"
                : $"/me/mix/{installation.AddonId}";
            var path = command.Path?.Trim('/');
            await bus.PublishAsync(new NotificationRequested(
                recipients,
                installation.AddonId,
                Category,
                command.Title,
                command.Body,
                string.IsNullOrEmpty(path) ? page : $"{page}/{path}",
                installation.TargetKind == TargetKind.Household ? installation.TargetId : null,
                command.Tag is null ? null : $"homemix:{installation.Id}:{command.Tag}"), cancellationToken);
        }

        return new PushNotificationResult(recipients.Count);
    }
}
