namespace UserManagement.Domain.Entities;

public enum UserRole
{
    Admin,
    HR,
    Employee
}

public enum LogoutReason
{
    UserLogout,
    TokenExpired,
    ForcedByAdmin,
    NewLoginElsewhere
}
