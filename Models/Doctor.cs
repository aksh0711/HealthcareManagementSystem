using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HealthcareManagementSystem.Attributes;

namespace HealthcareManagementSystem.Models
{
    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [PhoneNumber]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Specialization { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [StringLength(500)]
        public string Qualifications { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ConsultationFee { get; set; }

        public bool IsAvailable { get; set; } = true;

        [StringLength(1000)]
        public string Biography { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public virtual ICollection<LabTest> LabTests { get; set; } = new List<LabTest>();
        public virtual ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();

        [NotMapped]
        public string FullName => $"Dr. {FirstName} {LastName}";
    }
}
