using FluentValidation;

namespace Notifications.Application.Delivery.Deliver;

internal sealed class DeliverNotificationValidator : AbstractValidator<DeliverNotificationCommand>
{
    /// <summary>One message for a whole instance is fine; more means a bug in the sender.</summary>
    public const int MaxRecipients = 1000;

    public DeliverNotificationValidator()
    {
        RuleFor(c => c.Request).NotNull();
        RuleFor(c => c.Request.UserIds).NotEmpty().Must(ids => ids.Count <= MaxRecipients);
        RuleFor(c => c.Request.Source).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Request.Category).NotEmpty().MaximumLength(32);
        RuleFor(c => c.Request.Title).NotEmpty();
        RuleFor(c => c.Request.Body).NotEmpty();
        RuleFor(c => c.Request.Url).NotEmpty().MaximumLength(500)
            .Must(url => url.StartsWith('/') && !url.StartsWith("//", StringComparison.Ordinal))
            .WithMessage("Url must be a path of the web app, e.g. /homes/{id}.");
        RuleFor(c => c.Request.Tag).MaximumLength(64);
        RuleFor(c => c.Request.Urgency).IsInEnum();
    }
}
