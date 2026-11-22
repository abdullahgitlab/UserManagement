using UserManagement.Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace UserManagement.API.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException validation => (StatusCodes.Status400BadRequest, "Validation failed", string.Join(" ", validation.Errors.Select(error => error.ErrorMessage))),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Unauthorized", "Invalid username or password."),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden", "You are not authorized to perform this operation."),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "The request could not be completed.")
        };
        if (status == StatusCodes.Status500InternalServerError) logger.LogError(exception, "Unhandled API exception.");
        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        }, cancellationToken);
        return true;
    }
}
