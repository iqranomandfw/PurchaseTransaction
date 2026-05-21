using System.ComponentModel.DataAnnotations;

namespace PurchaseTransactionAPI.DTOs;

public class CreatePurchaseRequest
{
    [Required]
    [MaxLength(50)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly TransactionDate { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal PurchaseAmount { get; set; }
}
