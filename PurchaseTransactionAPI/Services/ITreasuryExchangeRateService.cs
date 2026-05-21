namespace PurchaseTransactionAPI.Services;

public interface ITreasuryExchangeRateService
{
    Task<TreasuryRateResult?> GetRateAsync(
        string countryCurrencyDesc,
        DateOnly purchaseDate,
        CancellationToken cancellationToken = default);
}

public class TreasuryRateResult
{
    public string CountryCurrencyDescription { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateOnly RecordDate { get; set; }
}
