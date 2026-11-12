using FluentValidation;

namespace HomeMix.Application.Notify.PushNotification;

internal sealed class PushNotificationValidator : AbstractValidator<PushNotificationCommand>
{
    public const int MaxRecipients = 50;

    public PushNotificationValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Body).NotEmpty().MaximumLength(300);
        RuleFor(c => c.UserIds!).Must(ids => ids.Count is > 0 and <= MaxRecipients)
            .When(c => c.UserIds is not null)
            .WithMessage($"Name 1 to {MaxRecipients} people, or none for everyone else.");
        RuleFor(c => c.Path!).Matches("^[A-Za-z0-9._~/-]{0,200}$").When(c => c.Path is not null);
        RuleFor(c => c.Tag!).Matches("^[A-Za-z0-9._-]{1,64}$").When(c => c.Tag is not null);
    }
}
