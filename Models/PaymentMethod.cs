using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public enum PaymentMethodType
    {
        Cash,
        CreditCard,
        DebitCard,
        BankTransfer,
        Check,
        DigitalWallet,
        InsuranceClaim,
        Other
    }

    public class PaymentMethod
    {
        [Key]
        public int PaymentMethodId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public PaymentMethodType Type { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public bool RequiresGateway { get; set; } = false;

        [StringLength(50)]
        public string GatewayProvider { get; set; } = string.Empty; // Stripe, PayPal, etc.

        [StringLength(1000)]
        public string GatewayConfiguration { get; set; } = string.Empty; // JSON config

        [Column(TypeName = "decimal(5,4)")]
        public decimal ProcessingFee { get; set; } = 0.0m; // Percentage fee

        [Column(TypeName = "decimal(10,2)")]
        public decimal FixedFee { get; set; } = 0.0m; // Fixed fee amount

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        [NotMapped]
        public string TypeDisplayName => Type switch
        {
            PaymentMethodType.Cash => "Cash",
            PaymentMethodType.CreditCard => "Credit Card",
            PaymentMethodType.DebitCard => "Debit Card",
            PaymentMethodType.BankTransfer => "Bank Transfer",
            PaymentMethodType.Check => "Check",
            PaymentMethodType.DigitalWallet => "Digital Wallet",
            PaymentMethodType.InsuranceClaim => "Insurance Claim",
            PaymentMethodType.Other => "Other",
            _ => "Unknown"
        };

        public decimal CalculateProcessingFee(decimal amount)
        {
            return (amount * ProcessingFee) + FixedFee;
        }
    }
}
