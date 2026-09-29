namespace InvestDashboard.Application.Exceptions;

public sealed class RegistrationConflictException : Exception
{
    public RegistrationConflictException() : base("An account with this email already exists.") { }
}
