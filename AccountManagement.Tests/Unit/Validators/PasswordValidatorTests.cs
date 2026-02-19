using FluentAssertions;
using Shared.Exceptions.Validators;
using Shared.Validators;

namespace AccountManagement.Tests.Unit.Validators;

public class PasswordValidatorTests {
    [Theory]
    [InlineData("Abcdef1!")]
    [InlineData("StrongPass1@")]
    [InlineData("Qwerty9#")]
    [InlineData("Aa1$aaaa")]
    public void Validate_ShouldPass_ForValidPasswords(string password) {
        var act = () => PasswordValidator.Validate(password);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("")]
    [InlineData("\n")]
    [InlineData("\t")]
    public void Validate_ShouldThrow_EmptyOrWhitespace(string password) {
        var act = () => PasswordValidator.Validate(password);

        act.Should().Throw<PasswordEmptyException>();
    }

    [Theory]
    [InlineData("Abcdef1! ")]
    [InlineData(" Abcdef1!")]
    [InlineData("Abc def1!")]
    [InlineData("Abcdef1!<")]
    [InlineData("Abcdef1!>")]
    public void Validate_ShouldThrow_ForWhitespaceOrForbiddenCharacters(string password) {
        var act = () => PasswordValidator.Validate(password);

        act.Should().Throw<PasswordContainsWhitespaceException>();
    }

    [Theory]
    [InlineData("abcdef1!")]
    [InlineData("password1@")]
    public void Validate_ShouldThrow_ForMissingUppercase(string password) {
        var act = () => PasswordValidator.Validate(password);

        act.Should().Throw<PasswordMissingUppercaseException>();
    }

    [Theory]
    [InlineData("ABCDEF1!")]
    [InlineData("PASSWORD1@")]
    public void Validate_ShouldThrow_ForMissingLowercase(string password) {
        var act = () => PasswordValidator.Validate(password);

        act.Should().Throw<PasswordMissingLowercaseException>();
    }
    
    [Theory]
    [InlineData("Abcdefg!")]
    [InlineData("Password@")]
    public void Validate_ShouldThrow_ForMissingDigit(string password) {
        var act = () => PasswordValidator.Validate(password);

        act.Should().Throw<PasswordMissingDigitException>();
    }

    [Theory]
    [InlineData("Abcdefg1")]
    [InlineData("Password1")]
    public void Validate_ShouldThrow_ForMissingSpecialCharacter(string password) {
        var act = () => PasswordValidator.Validate(password);

        act.Should().Throw<PasswordMissingSpecialCharacterException>();
    }
}