namespace InvestDashboard.Application.Exceptions;

public sealed class MarketDataUnavailableException : Exception
{
    public MarketDataUnavailableException() : base("Market data is temporarily unavailable.") { }
}
