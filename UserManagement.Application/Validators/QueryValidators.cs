using UserManagement.Application.Requests;
using FluentValidation;

namespace UserManagement.Application.Validators;

public sealed class GetUserSessionsValidator : AbstractValidator<GetUserSessionsQuery>
{
    public GetUserSessionsValidator()
    {
        RuleFor(item => item.UserId).NotEmpty();
        RuleFor(item => item.Page).GreaterThan(0);
        RuleFor(item => item.PageSize).InclusiveBetween(1, 200);
        RuleFor(item => item).Must(item => item.FromUtc is null || item.ToUtc is null || item.FromUtc <= item.ToUtc)
            .WithMessage("FromUtc must be earlier than or equal to ToUtc.");
    }
}

public sealed class GetLoginAttemptsValidator : AbstractValidator<GetLoginAttemptsQuery>
{
    public GetLoginAttemptsValidator()
    {
        RuleFor(item => item.Page).GreaterThan(0);
        RuleFor(item => item.PageSize).InclusiveBetween(1, 200);
        RuleFor(item => item).Must(item => item.FromUtc is null || item.ToUtc is null || item.FromUtc <= item.ToUtc)
            .WithMessage("FromUtc must be earlier than or equal to ToUtc.");
    }
}
