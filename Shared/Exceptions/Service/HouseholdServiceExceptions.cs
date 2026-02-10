using System.Net;

namespace Shared.Exceptions.Service;

public class HouseholdNotFoundException : ServiceException {
    public override string ErrorCode => "Household not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;

    public HouseholdNotFoundException(string message) : base(message) { }

    public HouseholdNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

public class HouseholdForbiddenException : ServiceException {
    public override string ErrorCode => "Household operation forbidden.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Forbidden;

    public HouseholdForbiddenException(string message) : base(message) { }

    public HouseholdForbiddenException(string message, Exception innerException) : base(message, innerException) { }
}

public class HouseholdConflictException : ServiceException {
    public override string ErrorCode => "Household operation conflict.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Conflict;

    public HouseholdConflictException(string message) : base(message) { }

    public HouseholdConflictException(string message, Exception innerException) : base(message, innerException) { }
}
