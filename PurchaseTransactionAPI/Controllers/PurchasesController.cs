using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchaseTransactionAPI.Data;
using PurchaseTransactionAPI.DTOs;
using PurchaseTransactionAPI.Models;
using PurchaseTransactionAPI.Services;

namespace PurchaseTransactionAPI.Controllers;

[ApiController]
[Route("api/purchases")]
public class PurchasesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITreasuryExchangeRateService _treasuryService;

    public PurchasesController(
        AppDbContext context,
        ITreasuryExchangeRateService treasuryService)
    {
        _context = context;
        _treasuryService = treasuryService;
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseResponse>> Create(
        CreatePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PurchaseAmount <= 0)
        {
            return BadRequest("Purchase amount must be positive.");
        }

        var purchase = new PurchaseTransaction
        {
            Id = Guid.NewGuid(),
            Description = request.Description.Trim(),
            TransactionDate = request.TransactionDate,
            PurchaseAmountUsd = Math.Round(request.PurchaseAmount, 2)
        };

        _context.PurchaseTransactions.Add(purchase);
        await _context.SaveChangesAsync(cancellationToken);

        var response = new PurchaseResponse
        {
            Id = purchase.Id,
            Description = purchase.Description,
            TransactionDate = purchase.TransactionDate,
            PurchaseAmountUsd = purchase.PurchaseAmountUsd
        };

        return CreatedAtAction(nameof(GetById), new { id = purchase.Id }, response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var purchase = await _context.PurchaseTransactions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (purchase == null)
            return NotFound();

        return new PurchaseResponse
        {
            Id = purchase.Id,
            Description = purchase.Description,
            TransactionDate = purchase.TransactionDate,
            PurchaseAmountUsd = purchase.PurchaseAmountUsd
        };
    }

    [HttpGet("{id:guid}/convert")]
    public async Task<ActionResult<ConvertedPurchaseResponse>> Convert(
        Guid id,
        [FromQuery] string countryCurrencyDesc,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(countryCurrencyDesc))
        {
            return BadRequest("countryCurrencyDesc is required. Example: Canada-Dollar");
        }

        var purchase = await _context.PurchaseTransactions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (purchase == null)
            return NotFound("Purchase transaction not found.");

        var rate = await _treasuryService.GetRateAsync(
            countryCurrencyDesc,
            purchase.TransactionDate,
            cancellationToken);

        if (rate == null)
        {
            return BadRequest(
                "The purchase cannot be converted to the target currency because no exchange rate is available within 6 months equal to or before the purchase date.");
        }

        var convertedAmount = Math.Round(
            purchase.PurchaseAmountUsd * rate.ExchangeRate,
            2);

        return new ConvertedPurchaseResponse
        {
            Id = purchase.Id,
            Description = purchase.Description,
            TransactionDate = purchase.TransactionDate,
            OriginalPurchaseAmountUsd = purchase.PurchaseAmountUsd,
            TargetCurrency = rate.CountryCurrencyDescription,
            ExchangeRate = rate.ExchangeRate,
            ExchangeRateDate = rate.RecordDate,
            ConvertedAmount = convertedAmount
        };
    }
}
