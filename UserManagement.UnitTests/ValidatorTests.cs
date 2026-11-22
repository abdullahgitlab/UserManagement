using UserManagement.Application.Requests;
using UserManagement.Application.Validators;
using UserManagement.Domain.Entities;
using FluentAssertions;

namespace UserManagement.UnitTests;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData("password1!", false)]
    [InlineData("PASSWORD1!", false)]
    [InlineData("Password!!", false)]
    [InlineData("Password1", false)]
    [InlineData("Pass1!", false)]
    [InlineData("Password1!", true)]
    public async Task Registration_password_rules_are_enforced(string password, bool expectedValid)
    {
        var result = await new RegisterUserValidator().ValidateAsync(new RegisterUserCommand("person", "person@example.com", password, UserRole.Employee, null));
        result.IsValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData("not-an-email", false)]
    [InlineData("person@example.com", true)]
    public async Task Registration_email_format_is_validated(string email, bool expectedValid)
    {
        var result = await new RegisterUserValidator().ValidateAsync(new RegisterUserCommand("person", email, "Password1!", UserRole.Employee, null));
        result.IsValid.Should().Be(expectedValid);
    }
}
