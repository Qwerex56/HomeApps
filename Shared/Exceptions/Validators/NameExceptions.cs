namespace Shared.Exceptions.Validators;

public sealed class NameFormatException : ValidationException {
    public override string ErrorCode => "Invalid name format.";

    public string Value { get; }

    public NameFormatException(string value) : base($"Invalid name '{value}'. Name must start with a letter.") {
        Value = value;
    }

    public NameFormatException(string value, Exception inner) : base(
        $"Invalid name '{value}'. Name must start with a letter.", inner) {
        Value = value;
    }
}

public sealed class NameTooShortException : ValidationException {
    public override string ErrorCode => "Name is too short.";

    public string Value { get; }

    public NameTooShortException(string value) : base($"Name '{value}' is too short. Minimum length is 3 characters.") {
        Value = value;
    }

    public NameTooShortException(string value, Exception inner) : base(
        $"Name '{value}' is too short. Minimum length is 3 characters.", inner) {
        Value = value;
    }
}

public sealed class NameIsNullException : ValidationException {
    public override string ErrorCode => "Name is null.";
    public string Value { get; } = string.Empty;

    public NameIsNullException() : base($"Name is null.") { }

    public NameIsNullException(string? value, Exception inner) : base($"Name '{value}' is null.", inner) { }
}