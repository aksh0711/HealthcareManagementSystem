using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HealthcareManagementSystem.Models
{
    public class PharmacyMedication
    {
        [Key]
        public int PharmacyMedicationId { get; set; }

        [Required]
        public int PharmacyId { get; set; }

        [Required]
        public int MedicationId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level cannot be negative")]
        public int MinimumStockLevel { get; set; } = 10;

        public DateTime? LastRestockedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(50)]
        public string? BatchNumber { get; set; }

        [StringLength(100)]
        public string? SupplierName { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PharmacyId")]
        public virtual Pharmacy Pharmacy { get; set; } = null!;

        [ForeignKey("MedicationId")]
        public virtual Medication Medication { get; set; } = null!;

        [NotMapped]
        public bool IsLowStock => StockQuantity <= MinimumStockLevel;

        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;

        [NotMapped]
        public bool IsExpiringSoon => ExpiryDate.HasValue && 
                                     ExpiryDate.Value >= DateTime.Today && 
                                     ExpiryDate.Value <= DateTime.Today.AddDays(30);

        [NotMapped]
        public string StockStatus
        {
            get
            {
                if (!IsAvailable) return "Unavailable";
                if (IsExpired) return "Expired";
                if (StockQuantity == 0) return "Out of Stock";
                if (IsLowStock) return "Low Stock";
                if (IsExpiringSoon) return "Expiring Soon";
                return "In Stock";
            }
        }
    }
}
