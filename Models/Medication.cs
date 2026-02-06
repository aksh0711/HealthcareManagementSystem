using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class Medication
    {
        [Key]
        public int MedicationId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? GenericName { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        [Required]
        [StringLength(20)]
        public string NDCNumber { get; set; } = string.Empty; // National Drug Code

        [StringLength(100)]
        public string? Strength { get; set; }

        [StringLength(100)]
        public string? DosageForm { get; set; } // Tablet, Capsule, Liquid, etc.

        [StringLength(50)]
        public string? Route { get; set; } // Oral, Topical, Injection, etc.

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(2000)]
        public string? SideEffects { get; set; }

        [StringLength(1000)]
        public string? Contraindications { get; set; }

        [StringLength(500)]
        public string? StorageInstructions { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [StringLength(50)]
        public string? Category { get; set; } // Antibiotic, Pain Reliever, etc.

        public bool RequiresPrescription { get; set; } = true;

        public bool IsControlledSubstance { get; set; } = false;

        [StringLength(10)]
        public string? ControlledSubstanceSchedule { get; set; } // I, II, III, IV, V

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<PharmacyMedication> PharmacyMedications { get; set; } = new List<PharmacyMedication>();
        public virtual ICollection<PrescriptionMedication> PrescriptionMedications { get; set; } = new List<PrescriptionMedication>();

        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(GenericName) && GenericName != Name 
            ? $"{Name} ({GenericName})" 
            : Name;

        [NotMapped]
        public string FullDescription => $"{DisplayName} - {Strength} {DosageForm}";
    }
}
