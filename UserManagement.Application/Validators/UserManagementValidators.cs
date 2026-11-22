using UserManagement.Application.Requests;
using FluentValidation;

namespace UserManagement.Application.Validators;

public static class PasswordRules
{
    public static IRuleBuilderOptions<T, string> Apply<T>(IRuleBuilder<T, string> rule) => rule
        .NotEmpty().MinimumLength(8).Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
}

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(item => item.Username).NotEmpty().MaximumLength(100);
        RuleFor(item => item.Email).NotEmpty().EmailAddress().MaximumLength(256);
        PasswordRules.Apply(RuleFor(item => item.Password));
        RuleFor(item => item.Role).IsInEnum();
    }
}

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(item => item.Username).NotEmpty().MaximumLength(100);
        RuleFor(item => item.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class LogoutValidator : AbstractValidator<LogoutCommand>;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator() => RuleFor(item => item.RefreshToken).NotEmpty().MaximumLength(4096);
}

public sealed class ForceLogoutUserValidator : AbstractValidator<ForceLogoutUserCommand>
{
    public ForceLogoutUserValidator() => RuleFor(item => item.UserId).NotEmpty();
}

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(item => item.CurrentPassword).NotEmpty();
        PasswordRules.Apply(RuleFor(item => item.NewPassword));
    }
}
