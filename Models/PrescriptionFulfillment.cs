using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public enum FulfillmentStatus
    {
        Pending,
        InProgress,
        Completed,
        PartiallyFilled,
        Cancelled,
        Refunded
    }

    public class PrescriptionFulfillment
    {
        [Key]
        public int FulfillmentId { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        [Required]
        public int PharmacyId { get; set; }

        public int? PrescriptionMedicationId { get; set; }

        [Required]
        [StringLength(50)]
        public string FulfillmentNumber { get; set; } = string.Empty;

        public int QuantityDispensed { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal InsuranceCoverage { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PatientPay { get; set; }

        public FulfillmentStatus Status { get; set; } = FulfillmentStatus.Pending;

        public DateTime FulfilledDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string FulfilledBy { get; set; } = string.Empty; // Pharmacist name

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public bool IsRefill { get; set; } = false;

        [StringLength(500)]
        public string DocumentPath { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PrescriptionId")]
        public virtual Prescription Prescription { get; set; } = null!;

        [ForeignKey("PharmacyId")]
        public virtual Pharmacy Pharmacy { get; set; } = null!;

        [ForeignKey("PrescriptionMedicationId")]
        public virtual PrescriptionMedication? PrescriptionMedication { get; set; }

        [NotMapped]
        public string StatusDisplayName => Status switch
        {
            FulfillmentStatus.Pending => "Pending",
            FulfillmentStatus.InProgress => "In Progress",
            FulfillmentStatus.Completed => "Completed",
            FulfillmentStatus.PartiallyFilled => "Partially Filled",
            FulfillmentStatus.Cancelled => "Cancelled",
            FulfillmentStatus.Refunded => "Refunded",
            _ => "Unknown"
        };

        [NotMapped]
        public decimal CoveragePercentage => TotalCost > 0 ? (InsuranceCoverage / TotalCost) * 100 : 0;
    }
}
