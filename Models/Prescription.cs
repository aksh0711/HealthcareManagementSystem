using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public int? MedicalRecordId { get; set; }

        [Required]
        [StringLength(200)]
        public string MedicationName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Dosage { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Frequency { get; set; } = string.Empty;

        [Required]
        public int DurationDays { get; set; }

        [StringLength(500)]
        public string Instructions { get; set; } = string.Empty;

        [StringLength(300)]
        public string SideEffects { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public int RefillsAllowed { get; set; } = 0;

        public DateTime PrescribedDate { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [ForeignKey("MedicalRecordId")]
        public virtual MedicalRecord? MedicalRecord { get; set; }

        public virtual ICollection<PrescriptionMedication> PrescriptionMedications { get; set; } = new List<PrescriptionMedication>();
        public virtual ICollection<PrescriptionFulfillment> PrescriptionFulfillments { get; set; } = new List<PrescriptionFulfillment>();

        [NotMapped]
        public bool IsExpired => EndDate.HasValue && EndDate.Value < DateTime.UtcNow;

        [NotMapped]
        public string DurationDescription => $"{DurationDays} day{(DurationDays != 1 ? "s" : "")}";

        [NotMapped]
        public bool IsFulfilled => PrescriptionFulfillments.Any();
    }
}
