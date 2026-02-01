namespace Shared.Validators;

public interface IValidator<in T> {
    public static abstract bool Validate(T value);

    public static abstract bool ValidateOrNull(T? value);
}