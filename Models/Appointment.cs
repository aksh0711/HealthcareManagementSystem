using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HealthcareManagementSystem.Models
{
    public enum AppointmentStatus
    {
        Scheduled,
        Confirmed,
        InProgress,
        Completed,
        Cancelled,
        NoShow
    }

    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        public int DurationMinutes { get; set; } = 30;

        [Required]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        [StringLength(200)]
        public string ReasonForVisit { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal Fee { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PatientId")]
        [ValidateNever]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        [ValidateNever]
        public virtual Doctor Doctor { get; set; } = null!;

        [ValidateNever]
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        [ValidateNever]
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        [ValidateNever]
        public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
        [ValidateNever]
        public virtual ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();

        [NotMapped]
        public DateTime EndDateTime => AppointmentDateTime.AddMinutes(DurationMinutes);

        [NotMapped]
        public string StatusDisplayName => Status switch
        {
            AppointmentStatus.Scheduled => "Scheduled",
            AppointmentStatus.Confirmed => "Confirmed",
            AppointmentStatus.InProgress => "In Progress",
            AppointmentStatus.Completed => "Completed",
            AppointmentStatus.Cancelled => "Cancelled",
            AppointmentStatus.NoShow => "No Show",
            _ => "Unknown"
        };
    }
}
