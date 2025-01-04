namespace ExpenseTracker.Models
{
    public class Transaction
    {
        public int Id { get; set; } 
        public string? Source { get; set; }
        public required decimal Amount { get; set; }
        public required DateTime Date { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        public required TransactionType Type { get; set; }
        public string? Notes { get; set; }

        // Debt-specific fields
        public DateTime? DueDate { get; set; }
        public DebtStatus? Status { get; set; }
    }

    public enum TransactionType
    {
        CashIn,
        CashOut,
        Debt
    }

    public enum DebtStatus
    {
        Paid,
        Unpaid
    }
}