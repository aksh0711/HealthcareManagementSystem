using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class MedicalRecord
    {
        [Key]
        public int MedicalRecordId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        public int? AppointmentId { get; set; }

        [Required]
        public DateTime RecordDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(200)]
        public string ChiefComplaint { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Symptoms { get; set; } = string.Empty;

        [StringLength(1000)]
        public string PhysicalExamination { get; set; } = string.Empty;

        [StringLength(500)]
        public string VitalSigns { get; set; } = string.Empty;

        [StringLength(500)]
        public string Diagnosis { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Treatment { get; set; } = string.Empty;

        [StringLength(1000)]
        public string LabResults { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Recommendations { get; set; } = string.Empty;

        [StringLength(500)]
        public string FollowUpInstructions { get; set; } = string.Empty;

        public DateTime? NextAppointmentDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}
