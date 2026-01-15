using FluentAssertions;
using Shared.Exceptions.Validators;
using Shared.Validators;

namespace AccountManagement.Tests.Unit.Validators;

public class EmailValidatorTests {
    [Theory]
    [InlineData("test.user@domain.pl")]
    [InlineData("test@domain.pl")]
    public void Validate_ShouldPass_ForValidEmails(string email) {
        var act = () => EmailValidator.Validate(email);

        act.Should().NotThrow();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    public void Validate_ShouldThrow_ForInvalidEmails(string email)
    {
        var act = () => EmailValidator.Validate(email);

        act.Should().Throw<EmailFormatException>();
    }

}