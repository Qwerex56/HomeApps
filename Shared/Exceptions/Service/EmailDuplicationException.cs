namespace Shared.Exceptions.Service;

public sealed class EmailDuplicationException : Exception {
    public string Value { get; }

    public EmailDuplicationException(string value) : base($"User with email '{value}' already exists.") {
        Value = value;
    }

    public EmailDuplicationException(string value, Exception inner) : base($"User with email '{value}' already exists.",
        inner) {
        Value = value;
    }
}