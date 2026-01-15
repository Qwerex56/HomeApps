using System.Text.RegularExpressions;
using Shared.Exceptions.Validators;

namespace Shared.Validators;

public class EmailValidator : IValidator<string> {
    public static bool Validate(string value) {
        const string pattern = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
        
        var result = Regex.Match(value, pattern);
        return result.Success ? true : throw new EmailFormatException(value);
    }
}