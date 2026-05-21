using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using PurchaseTransactionAPI.DTOs;
using PurchaseTransactionAPI.Services;

namespace PurchaseTransactionAPI.Tests;

public class PurchasesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory;

    public PurchasesControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_DescriptionOver50Chars_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithDatabase();

        var request = new
        {
            description = new string('x', 51),
            transactionDate = "2025-12-15",
            purchaseAmount = 100m
        };

        var response = await client.PostAsJsonAsync("/api/purchases", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_NegativeAmount_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithDatabase();

        var request = new
        {
            description = "Test item",
            transactionDate = "2025-12-15",
            purchaseAmount = -10m
        };

        var response = await client.PostAsJsonAsync("/api/purchases", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ZeroAmount_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithDatabase();

        var request = new
        {
            description = "Test item",
            transactionDate = "2025-12-15",
            purchaseAmount = 0m
        };

        var response = await client.PostAsJsonAsync("/api/purchases", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidPurchase_ReturnsCreatedWithSavedData()
    {
        var client = _factory.CreateClientWithDatabase();

        var request = new
        {
            description = "Laptop bag",
            transactionDate = "2025-12-15",
            purchaseAmount = 120.50m
        };

        var response = await client.PostAsJsonAsync("/api/purchases", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PurchaseResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal("Laptop bag", body.Description);
        Assert.Equal(new DateOnly(2025, 12, 15), body.TransactionDate);
        Assert.Equal(120.50m, body.PurchaseAmountUsd);
    }

    [Fact]
    public async Task GetById_UnknownPurchaseId_ReturnsNotFound()
    {
        var client = _factory.CreateClientWithDatabase();

        var response = await client.GetAsync($"/api/purchases/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Convert_ValidRateFound_ReturnsConvertedAmount()
    {
        var client = _factory.CreateClientWithDatabase();

        var createResponse = await client.PostAsJsonAsync("/api/purchases", new
        {
            description = "Laptop bag",
            transactionDate = "2025-12-15",
            purchaseAmount = 120.50m
        });

        var purchase = await createResponse.Content.ReadFromJsonAsync<PurchaseResponse>(JsonOptions);
        Assert.NotNull(purchase);

        _factory.TreasuryMock
            .Setup(s => s.GetRateAsync(
                "Canada-Dollar",
                purchase.TransactionDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TreasuryRateResult
            {
                CountryCurrencyDescription = "Canada-Dollar",
                ExchangeRate = 1.35m,
                RecordDate = new DateOnly(2025, 12, 1)
            });

        var response = await client.GetAsync(
            $"/api/purchases/{purchase.Id}/convert?countryCurrencyDesc=Canada-Dollar");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var converted = await response.Content.ReadFromJsonAsync<ConvertedPurchaseResponse>(JsonOptions);
        Assert.NotNull(converted);
        Assert.Equal(162.68m, converted.ConvertedAmount);
        Assert.Equal(1.35m, converted.ExchangeRate);
        Assert.Equal("Canada-Dollar", converted.TargetCurrency);
    }

    [Fact]
    public async Task Convert_NoRateWithinSixMonths_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithDatabase();

        var createResponse = await client.PostAsJsonAsync("/api/purchases", new
        {
            description = "Laptop bag",
            transactionDate = "2025-12-15",
            purchaseAmount = 120.50m
        });

        var purchase = await createResponse.Content.ReadFromJsonAsync<PurchaseResponse>(JsonOptions);
        Assert.NotNull(purchase);

        _factory.TreasuryMock
            .Setup(s => s.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TreasuryRateResult?)null);

        var response = await client.GetAsync(
            $"/api/purchases/{purchase.Id}/convert?countryCurrencyDesc=Canada-Dollar");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Convert_ConvertedAmount_HasTwoDecimalPlaces()
    {
        var client = _factory.CreateClientWithDatabase();

        var createResponse = await client.PostAsJsonAsync("/api/purchases", new
        {
            description = "Gadget",
            transactionDate = "2025-06-01",
            purchaseAmount = 10.33m
        });

        var purchase = await createResponse.Content.ReadFromJsonAsync<PurchaseResponse>(JsonOptions);
        Assert.NotNull(purchase);

        _factory.TreasuryMock
            .Setup(s => s.GetRateAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TreasuryRateResult
            {
                CountryCurrencyDescription = "Canada-Dollar",
                ExchangeRate = 1.333m,
                RecordDate = new DateOnly(2025, 5, 1)
            });

        var response = await client.GetAsync(
            $"/api/purchases/{purchase.Id}/convert?countryCurrencyDesc=Canada-Dollar");

        var converted = await response.Content.ReadFromJsonAsync<ConvertedPurchaseResponse>(JsonOptions);
        Assert.NotNull(converted);
        Assert.Equal(Math.Round(10.33m * 1.333m, 2), converted.ConvertedAmount);
        Assert.Equal(0, converted.ConvertedAmount % 0.01m);
    }
}
