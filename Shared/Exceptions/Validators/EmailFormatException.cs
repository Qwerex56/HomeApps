namespace Shared.Exceptions.Validators;

public class EmailFormatException : Exception {
    public string Value { get; }

    public EmailFormatException(string value) : base($"Invalid email '{value}'.") {
        Value = value;
    }

    public EmailFormatException(string value, Exception inner) : base(
        $"Invalid email '{value}'.", inner) {
        Value = value;
    }
}