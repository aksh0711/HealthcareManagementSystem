using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class PrescriptionMedication
    {
        [Key]
        public int PrescriptionMedicationId { get; set; }

        [Required]
        public int PrescriptionId { get; set; }

        [Required]
        public int MedicationId { get; set; }

        [Required]
        [StringLength(200)]
        public string Dosage { get; set; } = string.Empty; // e.g., "10mg twice daily"

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Days supply must be at least 1")]
        public int DaysSupply { get; set; }

        [StringLength(500)]
        public string Instructions { get; set; } = string.Empty; // e.g., "Take with food"

        [Range(0, int.MaxValue, ErrorMessage = "Refills remaining cannot be negative")]
        public int RefillsRemaining { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Total refills cannot be negative")]
        public int TotalRefills { get; set; } = 0;

        [StringLength(100)]
        public string? SubstitutionAllowed { get; set; } = "Yes"; // Yes, No, Generic Only

        [Column(TypeName = "decimal(10,2)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalPrice => UnitPrice.HasValue ? UnitPrice.Value * Quantity : null;

        public DateTime? LastDispensedDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Discontinued, Completed

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PrescriptionId")]
        public virtual Prescription Prescription { get; set; } = null!;

        [ForeignKey("MedicationId")]
        public virtual Medication Medication { get; set; } = null!;

        public virtual ICollection<PrescriptionFulfillment> PrescriptionFulfillments { get; set; } = new List<PrescriptionFulfillment>();

        [NotMapped]
        public bool CanRefill => RefillsRemaining > 0 && Status == "Active";

        [NotMapped]
        public DateTime? NextRefillDate => LastDispensedDate?.AddDays(DaysSupply);

        [NotMapped]
        public bool IsRefillDue => NextRefillDate.HasValue && NextRefillDate.Value <= DateTime.Today && CanRefill;

        [NotMapped]
        public string DisplayText => $"{Medication?.Name} - {Dosage} x {Quantity}";
    }
}
