using System.Text.RegularExpressions;
using Shared.Exceptions.Validators;

namespace Shared.Validators;

public class EmailValidator : IValidator<string> {
    public static bool Validate(string value) {
        const string pattern = @"^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
        
        var result = Regex.Match(value, pattern);
        return result.Success ? true : throw new EmailFormatException(value);
    }
}