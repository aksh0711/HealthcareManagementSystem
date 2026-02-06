using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class Pharmacy
    {
        [Key]
        public int PharmacyId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;

        [StringLength(50)]
        public string LicenseNumber { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [StringLength(1000)]
        public string ServicesOffered { get; set; } = string.Empty;

        [StringLength(200)]
        public string OperatingHours { get; set; } = string.Empty;

        public bool IsPartnerPharmacy { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Medication> Medications { get; set; } = new List<Medication>();
        public virtual ICollection<PrescriptionFulfillment> PrescriptionFulfillments { get; set; } = new List<PrescriptionFulfillment>();
    }
}
