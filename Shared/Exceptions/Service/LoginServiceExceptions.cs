using System.Net;

namespace Shared.Exceptions.Service;

public class UserNotFoundException : ServiceException {
    public override string ErrorCode => "User not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;

    public UserNotFoundException(string value) : base(
        $"User with value: {value} has not been found. Please check your credentials.") { }

    public UserNotFoundException(string value, Exception innerException) : base(
        $"User with {value} has not been found. Please check your credentials.", innerException) { }
}

public class InvalidUserCredentialException : ServiceException {
    public override string ErrorCode => "Invalid username or password.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;

    public InvalidUserCredentialException(string value) : base(
        $"Provided user credentials are invalid. Provided credentials: {value}.") { }

    public InvalidUserCredentialException(string value, Exception innerException) : base(
        $"Provided user credentials are invalid. Provided credentials: {value}.", innerException) { }
}

public class TokenNotFoundException : ServiceException {
    public override string ErrorCode => "Token not found.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;

    public TokenNotFoundException(string value) : base(
        $"Provided token is invalid. Provided token: {value}.") { }

    public TokenNotFoundException(string value, Exception innerException) :
        base($"Provided token is invalid. Provided token: {value}.", innerException) { }

    public TokenNotFoundException() : base("Provided token is invalid.") { }
}

public class TokenExpiredException : ServiceException {
    public override string ErrorCode => "Refresh token has expired.";
    public override HttpStatusCode HttpStatusCode => HttpStatusCode.Unauthorized;

    public TokenExpiredException() : base("Refresh token has expired.") { }

    public TokenExpiredException(string value) : base($"Provided token has expired. {value}.") { }

    public TokenExpiredException(string value, Exception inner) : base("Refresh token has expired.", inner) { }
}