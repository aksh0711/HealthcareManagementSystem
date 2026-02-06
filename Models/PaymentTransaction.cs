using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public enum TransactionType
    {
        Charge,
        Refund,
        PartialRefund,
        Chargeback,
        Dispute,
        Fee
    }

    public enum TransactionStatus
    {
        Pending,
        Authorized,
        Captured,
        Failed,
        Cancelled,
        Refunded,
        Disputed
    }

    public class PaymentTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int PaymentId { get; set; }

        [Required]
        [StringLength(100)]
        public string GatewayTransactionId { get; set; } = string.Empty;

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(3)]
        public string Currency { get; set; } = "USD";

        [Column(TypeName = "decimal(10,2)")]
        public decimal GatewayFee { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal NetAmount { get; set; }

        [StringLength(50)]
        public string Gateway { get; set; } = string.Empty;

        [StringLength(2000)]
        public string GatewayResponse { get; set; } = string.Empty; // JSON response

        [StringLength(1000)]
        public string FailureReason { get; set; } = string.Empty;

        [StringLength(100)]
        public string AuthorizationCode { get; set; } = string.Empty;

        [StringLength(50)]
        public string ReferenceNumber { get; set; } = string.Empty;

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PaymentId")]
        public virtual Payment Payment { get; set; } = null!;

        [NotMapped]
        public string TypeDisplayName => Type switch
        {
            TransactionType.Charge => "Charge",
            TransactionType.Refund => "Refund",
            TransactionType.PartialRefund => "Partial Refund",
            TransactionType.Chargeback => "Chargeback",
            TransactionType.Dispute => "Dispute",
            TransactionType.Fee => "Fee",
            _ => "Unknown"
        };

        [NotMapped]
        public string StatusDisplayName => Status switch
        {
            TransactionStatus.Pending => "Pending",
            TransactionStatus.Authorized => "Authorized",
            TransactionStatus.Captured => "Captured",
            TransactionStatus.Failed => "Failed",
            TransactionStatus.Cancelled => "Cancelled",
            TransactionStatus.Refunded => "Refunded",
            TransactionStatus.Disputed => "Disputed",
            _ => "Unknown"
        };

        [NotMapped]
        public bool IsSuccessful => Status == TransactionStatus.Captured || Status == TransactionStatus.Authorized;
    }
}
