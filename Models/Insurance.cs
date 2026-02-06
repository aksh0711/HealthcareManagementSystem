using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class Insurance
    {
        [Key]
        public int InsuranceId { get; set; }

        [Required(ErrorMessage = "Please select a patient")]
        [Display(Name = "Patient")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a patient")]
        public int PatientId { get; set; }

        [Required]
        [StringLength(100)]
        public string ProviderName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string GroupNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string PrimaryInsuredName { get; set; } = string.Empty;

        [StringLength(100)]
        public string RelationshipToPrimary { get; set; } = string.Empty;

        [Required]
        public DateTime EffectiveDate { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal CopayAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DeductibleAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal CoveragePercentage { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Document upload fields
        [StringLength(255)]
        public string? FrontCardImagePath { get; set; }

        [StringLength(255)]
        public string? BackCardImagePath { get; set; }

        [StringLength(500)]
        public string? AdditionalDocumentsPath { get; set; }

        public DateTime? DocumentsUploadedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        [NotMapped]
        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.Today;

        [NotMapped]
        public string Status => IsActive ? (IsExpired ? "Expired" : "Active") : "Inactive";
    }
}
