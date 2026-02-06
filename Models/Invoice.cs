using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Overdue,
        Cancelled,
        PartiallyPaid
    }

    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int PatientId { get; set; }

        public int? AppointmentId { get; set; }

        public int? InsuranceId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal InsuranceCovered { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public DateTime? PaidDate { get; set; }

        [StringLength(100)]
        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        [ForeignKey("InsuranceId")]
        public virtual Insurance? Insurance { get; set; }

        public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        [NotMapped]
        public decimal BalanceAmount => TotalAmount - AmountPaid;

        [NotMapped]
        public bool IsOverdue => Status != InvoiceStatus.Paid && Status != InvoiceStatus.Cancelled && DueDate < DateTime.Today;

        [NotMapped]
        public string StatusDisplayName => Status switch
        {
            InvoiceStatus.Draft => "Draft",
            InvoiceStatus.Sent => "Sent",
            InvoiceStatus.Paid => "Paid",
            InvoiceStatus.Overdue => "Overdue",
            InvoiceStatus.Cancelled => "Cancelled",
            InvoiceStatus.PartiallyPaid => "Partially Paid",
            _ => "Unknown"
        };
    }
}
