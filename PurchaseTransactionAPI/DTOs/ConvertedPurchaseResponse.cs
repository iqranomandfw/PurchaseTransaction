namespace PurchaseTransactionAPI.DTOs;

public class ConvertedPurchaseResponse
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly TransactionDate { get; set; }
    public decimal OriginalPurchaseAmountUsd { get; set; }
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateOnly ExchangeRateDate { get; set; }
    public decimal ConvertedAmount { get; set; }
}
