using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PurchaseTransactionAPI.Services;

public class TreasuryExchangeRateService : ITreasuryExchangeRateService
{
    private readonly HttpClient _httpClient;

    public TreasuryExchangeRateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TreasuryRateResult?> GetRateAsync(
        string countryCurrencyDesc,
        DateOnly purchaseDate,
        CancellationToken cancellationToken = default)
    {
        var sixMonthsBefore = purchaseDate.AddMonths(-6);

        var url =
            "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange" +
            "?fields=country_currency_desc,exchange_rate,record_date" +
            $"&filter=country_currency_desc:eq:{Uri.EscapeDataString(countryCurrencyDesc)}," +
            $"record_date:lte:{purchaseDate:yyyy-MM-dd}," +
            $"record_date:gte:{sixMonthsBefore:yyyy-MM-dd}" +
            "&sort=-record_date" +
            "&page[size]=1";

        var response = await _httpClient.GetFromJsonAsync<TreasuryApiResponse>(
            url,
            cancellationToken);

        var item = response?.Data?.FirstOrDefault();

        if (item == null)
            return null;

        return new TreasuryRateResult
        {
            CountryCurrencyDescription = item.CountryCurrencyDescription,
            ExchangeRate = decimal.Parse(item.ExchangeRate),
            RecordDate = DateOnly.Parse(item.RecordDate)
        };
    }
}

public class TreasuryApiResponse
{
    [JsonPropertyName("data")]
    public List<TreasuryRateItem> Data { get; set; } = new();
}

public class TreasuryRateItem
{
    [JsonPropertyName("country_currency_desc")]
    public string CountryCurrencyDescription { get; set; } = string.Empty;

    [JsonPropertyName("exchange_rate")]
    public string ExchangeRate { get; set; } = string.Empty;

    [JsonPropertyName("record_date")]
    public string RecordDate { get; set; } = string.Empty;
}
