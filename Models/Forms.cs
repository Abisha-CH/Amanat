using System.ComponentModel.DataAnnotations;

namespace Amanat.Models;

public sealed class PledgeInput
{
    public string CampaignSlug { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your name.")]
    [Display(Name = "Your Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a valid amount.")]
    [Range(1, 100000000, ErrorMessage = "Amount must be at least PKR 1.")]
    [Display(Name = "Amount (PKR)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Please enter your transaction reference.")]
    [Display(Name = "Transaction Ref / TRX ID")]
    public string TransactionRef { get; set; } = string.Empty;
}

public sealed class PaymentReferenceInput
{
    [Required]
    public string Campaign { get; set; } = string.Empty;

    [Required]
    [Range(1, 100000000)]
    public decimal Amount { get; set; }

    [Required]
    public string TransactionId { get; set; } = string.Empty;

    public string DateSent { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class ExpenseInput
{
    [Required]
    public string Campaign { get; set; } = string.Empty;

    [Required]
    [Range(1, 100000000)]
    public decimal Amount { get; set; }

    [Required]
    public string PayeeName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
