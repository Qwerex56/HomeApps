namespace Shared.Validators;

public interface IValidator<in T> {
    public static abstract bool Validate(T value);
}