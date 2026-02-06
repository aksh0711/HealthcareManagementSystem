using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class LabTest
    {
        [Key]
        public int LabTestId { get; set; }

        [Required]
        public int LaboratoryId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int? DoctorId { get; set; }

        [Required]
        [StringLength(50)]
        public string TestCode { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TestName { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Cost { get; set; }

        [Required]
        public DateTime OrderedDate { get; set; }

        public DateTime? CollectedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Ordered"; // Ordered, Collected, InProgress, Completed, Cancelled

        [StringLength(1000)]
        public string? Results { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(50)]
        public string Priority { get; set; } = "Normal"; // Urgent, High, Normal, Low

        public bool IsAbnormal { get; set; } = false;

        [StringLength(255)]
        public string? ResultsFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("LaboratoryId")]
        public virtual Laboratory Laboratory { get; set; } = null!;

        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        [NotMapped]
        public bool IsCompleted => Status == "Completed";

        [NotMapped]
        public bool IsOverdue => Status != "Completed" && Status != "Cancelled" && OrderedDate.AddDays(7) < DateTime.Today;
    }
}
