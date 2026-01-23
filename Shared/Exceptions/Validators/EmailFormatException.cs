namespace Shared.Exceptions.Validators;

public sealed class EmailFormatException : ValidationException {
    public override string ErrorCode => "Invalid Email format.";

    public string Value { get; }

    public EmailFormatException(string value) : base($"Invalid email '{value}'.") {
        Value = value;
    }

    public EmailFormatException(string value, Exception inner) : base(
        $"Invalid email '{value}'.", inner) {
        Value = value;
    }
}