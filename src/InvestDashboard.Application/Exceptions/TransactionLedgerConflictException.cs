namespace InvestDashboard.Application.Exceptions;

public sealed class TransactionLedgerConflictException : Exception
{
    public TransactionLedgerConflictException() : base("This change conflicts with later transactions. Correct dependent transactions first.") { }
}
