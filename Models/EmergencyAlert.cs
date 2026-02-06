using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public enum EmergencyAlertLevel
    {
        Low,
        Medium, 
        High,
        Critical
    }

    public enum EmergencyAlertType
    {
        Medical,
        Fire,
        Security,
        Weather,
        Evacuation,
        System
    }

    public enum EmergencyAlertTarget
    {
        All,
        AllPatients,
        ActivePatients,
        Patients,
        Doctors,
        Staff,
        Department,
        SpecificPatients,
        Specific
    }

    public enum EmergencyAlertStatus
    {
        Draft,
        Active,
        Inactive,
        Cancelled,
        Completed
    }

    public enum EmergencyAlertRecipientStatus
    {
        Pending,
        Sent,
        Delivered,
        Read,
        ReadConfirmed,
        Failed
    }

    public class EmergencyAlert
    {
        [Key]
        public int EmergencyAlertId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        public EmergencyAlertLevel Level { get; set; }

        [Required]
        public EmergencyAlertType Type { get; set; }

        [Required]
        public EmergencyAlertTarget Target { get; set; }

        public EmergencyAlertTarget TargetAudience { get; set; }
        public EmergencyAlertStatus Status { get; set; } = EmergencyAlertStatus.Draft;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ActivatedDate { get; set; }
        public DateTime? DeactivatedDate { get; set; }
        public string AuthorizedBy { get; set; } = string.Empty;
        public bool RequiresReadConfirmation { get; set; } = false;
        public int TotalRecipients { get; set; } = 0;
        public int SuccessfulCount { get; set; } = 0;
        public int FailedCount { get; set; } = 0;
        public int ReadConfirmationCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        [StringLength(100)]
        public string ResolvedBy { get; set; } = string.Empty;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; } = 0.00m;

        // Navigation properties
        public virtual ICollection<EmergencyAlertRecipient> Recipients { get; set; } = new List<EmergencyAlertRecipient>();
    }

    public class EmergencyAlertRecipient
    {
        [Key]
        public int EmergencyAlertRecipientId { get; set; }

        [Required]
        public int EmergencyAlertId { get; set; }

        [Required]
        [StringLength(100)]
        public string RecipientType { get; set; } = string.Empty; // Patient, Doctor, Staff

        [Required]
        public int RecipientId { get; set; }

        [Required]
        [StringLength(200)]
        public string RecipientName { get; set; } = string.Empty;

        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool NotificationSent { get; set; } = false;
        public DateTime? SentAt { get; set; }
        public DateTime? SentDate { get; set; }
        public DateTime? ReadConfirmedDate { get; set; }

        public EmergencyAlertRecipientStatus Status { get; set; } = EmergencyAlertRecipientStatus.Pending;
        
        [StringLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;

        public int PatientId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual EmergencyAlert EmergencyAlert { get; set; } = null!;
        public virtual Patient? Patient { get; set; }
    }
}
