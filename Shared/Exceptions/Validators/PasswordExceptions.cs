namespace Shared.Exceptions.Validators;

public sealed class PasswordEmptyException : ValidationException {
    public override string ErrorCode => "Password is empty.";

    public PasswordEmptyException()
        : base("Password cannot be empty.") {
    }

    public PasswordEmptyException(string message) : base("Password cannot be empty.") {
    }

    public PasswordEmptyException(string message, Exception inner) : base(message, inner) {
    }
}

public sealed class PasswordTooShortException : ValidationException {
    public override string ErrorCode => "Password is too short.";

    public PasswordTooShortException(string value)
        : base($"Password '{value}' is too short. Minimum length is 8 characters.") {
    }

    public PasswordTooShortException(string value, Exception inner) : base(
        $"Password '{value}' is too short. Minimum length is 8 characters.", inner) {
    }
}

public sealed class PasswordContainsWhitespaceException : ValidationException {
    public override string ErrorCode => "Password is contained whitespace.";

    public PasswordContainsWhitespaceException(string value)
        : base($"Password '{value}' cannot contain spaces.") {
    }

    public PasswordContainsWhitespaceException(string value, Exception inner)
        : base($"Password '{value}' cannot contain spaces.", inner) {
    }
}

public sealed class PasswordMissingUppercaseException : ValidationException {
    public override string ErrorCode => "Password is missing uppercase character.";

    public PasswordMissingUppercaseException(string value)
        : base($"Password '{value}' must contain at least one uppercase letter.") {
    }

    public PasswordMissingUppercaseException(string value, Exception inner)
        : base($"Password '{value}' must contain at least one uppercase letter.", inner) {
    }
}

public sealed class PasswordMissingLowercaseException : ValidationException {
    public override string ErrorCode => "Password is missing lower case character.";

    public PasswordMissingLowercaseException(string value)
        : base($"Password '{value}' must contain at least one lowercase letter.") {
    }

    public PasswordMissingLowercaseException(string value, Exception inner)
        : base($"Password '{value}' must contain at least one lowercase letter.", inner) {
    }
}

public sealed class PasswordMissingDigitException : ValidationException {
    public override string ErrorCode => "Password is missing digit.";

    public PasswordMissingDigitException(string value)
        : base($"Password '{value}' must contain at least one digit.") {
    }

    public PasswordMissingDigitException(string value, Exception inner)
        : base($"Password '{value}' must contain at least one digit", inner) {
    }
}

public sealed class PasswordMissingSpecialCharacterException : ValidationException {
    public override string ErrorCode => "Password is missing special character.";

    public PasswordMissingSpecialCharacterException(string value)
        : base($"Password '{value}' must contain at least one special character.") {
    }

    public PasswordMissingSpecialCharacterException(string value, Exception inner)
        : base($"Password '{value}' must contain at least one special character.", inner) {
    }
}