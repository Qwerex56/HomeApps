using FluentResults;
using HomeApps.BuildingBlocks.Domain.Errors;

namespace Auth.Application.Delegation.DelegateToAssistant;

public static class DelegationErrors
{
    public static Error ClientInvalid => new AppError(
        "delegation.client_invalid", "The service client or its secret is not valid.", ErrorKind.Unauthorized);

    /// <summary>Unknown, disabled or erased: the assistant cannot act for this person.</summary>
    public static Error UserUnavailable => new AppError(
        "delegation.user_unavailable", "This person's account is not active.", ErrorKind.NotFound);
}
