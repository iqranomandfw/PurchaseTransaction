namespace PurchaseTransactionAPI.DTOs;

public class PurchaseResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public decimal PurchaseAmountUsd { get; set; }
}
