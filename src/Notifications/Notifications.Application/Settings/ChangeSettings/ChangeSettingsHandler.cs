using System.Globalization;
using FluentResults;
using HomeApps.BuildingBlocks.Application.Abstractions;
using HomeApps.BuildingBlocks.Application.Messaging;
using Notifications.Application.Abstractions;
using Notifications.Domain.Recipients;

namespace Notifications.Application.Settings.ChangeSettings;

internal sealed class ChangeSettingsHandler(IRecipientRepository recipients, ICurrentUser currentUser)
    : ICommandHandler<ChangeSettingsCommand>
{
    public async Task<Result> Handle(ChangeSettingsCommand command, CancellationToken cancellationToken)
    {
        QuietHours? quiet = null;
        if (command.QuietHours is { } input)
        {
            var created = QuietHours.Create(Parse(input.From), Parse(input.To), input.TimeZone);
            if (created.IsFailed)
            {
                return created.ToResult();
            }

            quiet = created.Value;
        }

        var recipient = await recipients.GetOrCreateAsync(currentUser.RequiredUserId, cancellationToken);
        recipient.ChangeSettings(command.ShowPreview, quiet);
        return Result.Ok();
    }

    private static TimeOnly Parse(string value) => TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture);
}
