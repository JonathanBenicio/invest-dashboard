namespace InvestDashboard.Application.Exceptions;

public sealed class IdentityProviderUnavailableException : Exception
{
    public IdentityProviderUnavailableException() : base("The identity provider is temporarily unavailable.") { }
}
