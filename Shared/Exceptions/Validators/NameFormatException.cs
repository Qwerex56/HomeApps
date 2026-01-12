namespace Shared.Exceptions.Validators;

public sealed class NameFormatException : Exception {
    public string Value { get; }

    public NameFormatException(string value) : base($"Invalid name '{value}'. Name must start with a letter.") {
        Value = value;
    }

    public NameFormatException(string value, Exception inner) : base(
        $"Invalid name '{value}'. Name must start with a letter.", inner) {
        Value = value;
    }
}