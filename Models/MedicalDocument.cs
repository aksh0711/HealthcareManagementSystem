using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public enum DocumentType
    {
        MedicalHistory,
        LabResults,
        XRay,
        MRI,
        CTScan,
        Ultrasound,
        Prescription,
        Invoice,
        InsuranceCard,
        Consent,
        Discharge,
        Referral,
        Other
    }

    public class MedicalDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        public int? DoctorId { get; set; }

        public int? AppointmentId { get; set; }

        public int? LabTestId { get; set; }

        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        public DocumentType DocumentType { get; set; }

        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime DocumentDate { get; set; } = DateTime.UtcNow;

        public bool IsConfidential { get; set; } = true;

        [StringLength(100)]
        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string Tags { get; set; } = string.Empty;

        public bool IsArchived { get; set; } = false;

        public DateTime? ArchivedAt { get; set; }

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        [ForeignKey("LabTestId")]
        public virtual LabTest? LabTest { get; set; }

        [NotMapped]
        public string DocumentTypeDisplayName => DocumentType switch
        {
            DocumentType.MedicalHistory => "Medical History",
            DocumentType.LabResults => "Lab Results",
            DocumentType.XRay => "X-Ray",
            DocumentType.MRI => "MRI",
            DocumentType.CTScan => "CT Scan",
            DocumentType.Ultrasound => "Ultrasound",
            DocumentType.Prescription => "Prescription",
            DocumentType.Invoice => "Invoice",
            DocumentType.InsuranceCard => "Insurance Card",
            DocumentType.Consent => "Consent Form",
            DocumentType.Discharge => "Discharge Summary",
            DocumentType.Referral => "Referral",
            DocumentType.Other => "Other",
            _ => "Unknown"
        };

        [NotMapped]
        public string FileExtension => Path.GetExtension(FileName);

        [NotMapped]
        public string FileSizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        [NotMapped]
        public bool IsImage => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }
            .Contains(FileExtension.ToLower());

        [NotMapped]
        public bool IsPdf => FileExtension.ToLower() == ".pdf";
    }
}
