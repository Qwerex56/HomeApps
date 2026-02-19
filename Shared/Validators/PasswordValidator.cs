using System.Text.RegularExpressions;
using Shared.Exceptions.Validators;

namespace Shared.Validators;

public class PasswordValidator : IValidator<string> {
    public static bool Validate(string value) {
        if (string.IsNullOrWhiteSpace(value))
            throw new PasswordEmptyException();

        if (value.Length < 8) {
            throw new PasswordTooShortException(value);
        }

        if (Regex.IsMatch(value, @"[\s<>]")) {
            throw new PasswordContainsWhitespaceException(value);
        }

        if (!Regex.IsMatch(value, "[A-Z]")) {
            throw new PasswordMissingUppercaseException(value);
        }

        if (!Regex.IsMatch(value, "[a-z]")) {
            throw new PasswordMissingLowercaseException(value);
        }

        if (!Regex.IsMatch(value, "[0-9]")) {
            throw new PasswordMissingDigitException(value);
        }

        if (!Regex.IsMatch(value, @"(?=.*[@$!%*?&^#()[\]{}|\\/\-+_.:;=,~])")) {
            throw new PasswordMissingSpecialCharacterException(value);
        }

        return true;
    }

    public static bool ValidateOrNull(string? value) {
        if (value is null) {
            throw new PasswordIsNullException();
        }
        
        return Validate(value);
    }
}