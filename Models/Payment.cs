using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Cancelled,
        Refunded,
        PartiallyRefunded
    }

    public enum PaymentType
    {
        Full,
        Partial,
        Refund,
        Adjustment
    }

    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentNumber { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Required]
        public PaymentType Type { get; set; } = PaymentType.Full;

        [Required]
        public int PaymentMethodId { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string TransactionId { get; set; } = string.Empty;

        [StringLength(100)]
        public string GatewayTransactionId { get; set; } = string.Empty;

        [StringLength(50)]
        public string Gateway { get; set; } = string.Empty; // Stripe, PayPal, etc.

        [Column(TypeName = "decimal(5,4)")]
        public decimal GatewayFee { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal NetAmount { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        [StringLength(1000)]
        public string FailureReason { get; set; } = string.Empty;

        public DateTime? ProcessedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("InvoiceId")]
        public virtual Invoice Invoice { get; set; } = null!;

        [ForeignKey("PaymentMethodId")]
        public virtual PaymentMethod PaymentMethod { get; set; } = null!;

        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

        [NotMapped]
        public string StatusDisplayName => Status switch
        {
            PaymentStatus.Pending => "Pending",
            PaymentStatus.Processing => "Processing",
            PaymentStatus.Completed => "Completed",
            PaymentStatus.Failed => "Failed",
            PaymentStatus.Cancelled => "Cancelled",
            PaymentStatus.Refunded => "Refunded",
            PaymentStatus.PartiallyRefunded => "Partially Refunded",
            _ => "Unknown"
        };

        [NotMapped]
        public string TypeDisplayName => Type switch
        {
            PaymentType.Full => "Full Payment",
            PaymentType.Partial => "Partial Payment",
            PaymentType.Refund => "Refund",
            PaymentType.Adjustment => "Adjustment",
            _ => "Unknown"
        };
    }
}
