namespace UserManagement.Application.Exceptions;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException() : base("Invalid username or password.") { }
}

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base("You are not authorized to perform this operation.") { }
}

public sealed class NotFoundException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
