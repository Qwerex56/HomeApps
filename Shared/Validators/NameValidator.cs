using System.Text.RegularExpressions;
using Shared.Exceptions.Validators;

namespace Shared.Validators;

public class NameValidator : IValidator<string> {
    public static bool Validate(string value) {
        if (value.Length < 3) {
            throw new NameTooShortException(value);
        }
        
        const string pattern = @"^[a-zA-Z][a-zA-Z0-9_]*$";

        var result = Regex.Match(value, pattern);

        return result.Success ? true : throw new NameFormatException(value);
    }
}