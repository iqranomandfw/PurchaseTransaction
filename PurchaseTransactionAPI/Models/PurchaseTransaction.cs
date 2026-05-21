using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace PurchaseTransactionAPI.Models;

public class PurchaseTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Description { get; set; } = string.Empty;

    public DateOnly TransactionDate { get; set; }

    [Precision(18, 2)]
    public decimal PurchaseAmountUsd { get; set; }
}
