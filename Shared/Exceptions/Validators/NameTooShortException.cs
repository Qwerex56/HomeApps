namespace Shared.Exceptions.Validators;

public sealed class NameTooShortException : Exception {
    public string Value { get; }

    public NameTooShortException(string value) : base($"Name '{value}' is too short. Minimum length is 3 characters.") {
        Value = value;
    }

    public NameTooShortException(string value, Exception inner) : base(
        $"Name '{value}' is too short. Minimum length is 3 characters.", inner) {
        Value = value;
    }
}