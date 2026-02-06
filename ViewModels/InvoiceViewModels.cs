using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using HealthcareManagementSystem.Models;

namespace HealthcareManagementSystem.ViewModels
{
    public class InvoiceIndexViewModel
    {
        public List<Invoice> Invoices { get; set; } = new();
        public string? SearchString { get; set; }
        public InvoiceStatus? SelectedStatus { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<SelectListItem> StatusList { get; set; } = new();

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class InvoiceDetailsViewModel
    {
        public Invoice Invoice { get; set; } = null!;
        public List<Payment> Payments { get; set; } = new();
        public List<SelectListItem> PaymentMethods { get; set; } = new();
    }

    public class InvoiceCreateViewModel
    {
        [Required(ErrorMessage = "Please select a patient.")]
        [Display(Name = "Patient")]
        public int? PatientId { get; set; }

        [Display(Name = "Patient")]
        public string? PatientName { get; set; }

        public int? AppointmentId { get; set; }

        [Display(Name = "Appointment")]
        public string? AppointmentDisplay { get; set; }

        public int? InsuranceId { get; set; }

        [Display(Name = "Insurance")]
        public string? InsuranceName { get; set; }

        [Required]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Tax Amount")]
        [Range(0, 99999.99, ErrorMessage = "Tax amount must be between 0 and 99999.99")]
        [DataType(DataType.Currency)]
        public decimal TaxAmount { get; set; }

        [Display(Name = "Discount Amount")]
        [Range(0, 99999.99, ErrorMessage = "Discount amount must be between 0 and 99999.99")]
        [DataType(DataType.Currency)]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }
    }

    public class InvoiceEditViewModel
    {
        public int InvoiceId { get; set; }

        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int PatientId { get; set; }

        [Display(Name = "Patient")]
        public string PatientName { get; set; } = string.Empty;

        public int? AppointmentId { get; set; }

        [Display(Name = "Appointment")]
        public string? AppointmentDisplay { get; set; }

        public int? InsuranceId { get; set; }

        [Display(Name = "Insurance")]
        public string? InsuranceName { get; set; }

        [Required]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Status")]
        public InvoiceStatus Status { get; set; }

        [Display(Name = "Tax Amount")]
        [Range(0, 99999.99, ErrorMessage = "Tax amount must be between 0 and 99999.99")]
        [DataType(DataType.Currency)]
        public decimal TaxAmount { get; set; }

        [Display(Name = "Discount Amount")]
        [Range(0, 99999.99, ErrorMessage = "Discount amount must be between 0 and 99999.99")]
        [DataType(DataType.Currency)]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties for display
        public Invoice? Invoice { get; set; }
        public List<InvoiceItem> InvoiceItems { get; set; } = new();
    }

    public class InvoiceDashboardViewModel
    {
        [Display(Name = "Total Revenue (Last 30 Days)")]
        [DataType(DataType.Currency)]
        public decimal TotalRevenue { get; set; }

        [Display(Name = "Outstanding Balance")]
        [DataType(DataType.Currency)]
        public decimal OutstandingBalance { get; set; }

        [Display(Name = "Overdue Invoices")]
        public int OverdueInvoicesCount { get; set; }

        [Display(Name = "Recent Invoices")]
        public int RecentInvoicesCount { get; set; }

        public List<Invoice> OverdueInvoices { get; set; } = new();
        public List<Invoice> RecentInvoices { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
    }

    public class PaymentCreateViewModel
    {
        public int InvoiceId { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public int PaymentMethodId { get; set; }

        [Required]
        [Display(Name = "Amount")]
        [Range(0.01, 99999.99, ErrorMessage = "Amount must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Display(Name = "Payment Type")]
        public PaymentType Type { get; set; } = PaymentType.Full;

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        // For credit card payments
        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiry Month")]
        [Range(1, 12)]
        public int? ExpiryMonth { get; set; }

        [Display(Name = "Expiry Year")]
        [Range(2024, 2050)]
        public int? ExpiryYear { get; set; }

        [Display(Name = "CVC")]
        public string? CVC { get; set; }

        [Display(Name = "Cardholder Name")]
        public string? CardholderName { get; set; }

        // For display
        public Invoice? Invoice { get; set; }
        public List<SelectListItem> PaymentMethods { get; set; } = new();
    }

    public class BillingDashboardViewModel
    {
        [Display(Name = "Today's Revenue")]
        [DataType(DataType.Currency)]
        public decimal TodayRevenue { get; set; }

        [Display(Name = "This Month's Revenue")]
        [DataType(DataType.Currency)]
        public decimal MonthRevenue { get; set; }

        [Display(Name = "This Year's Revenue")]
        [DataType(DataType.Currency)]
        public decimal YearRevenue { get; set; }

        [Display(Name = "Outstanding Balance")]
        [DataType(DataType.Currency)]
        public decimal OutstandingBalance { get; set; }

        [Display(Name = "Pending Payments")]
        public int PendingPaymentsCount { get; set; }

        [Display(Name = "Overdue Invoices")]
        public int OverdueInvoicesCount { get; set; }

        [Display(Name = "Failed Payments")]
        public int FailedPaymentsCount { get; set; }

        public List<Invoice> RecentInvoices { get; set; } = new();
        public List<Payment> RecentPayments { get; set; } = new();
        public List<Invoice> OverdueInvoices { get; set; } = new();
        public Dictionary<string, decimal> RevenueByMonth { get; set; } = new();
        public Dictionary<string, decimal> RevenueByPaymentMethod { get; set; } = new();
    }

    public class PaymentMethodViewModel
    {
        public int PaymentMethodId { get; set; }

        [Required]
        [Display(Name = "Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Type")]
        public PaymentMethodType Type { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Requires Gateway")]
        public bool RequiresGateway { get; set; }

        [Display(Name = "Gateway Provider")]
        [StringLength(50)]
        public string? GatewayProvider { get; set; }

        [Display(Name = "Processing Fee (%)")]
        [Range(0, 1, ErrorMessage = "Processing fee must be between 0 and 1 (0-100%)")]
        public decimal ProcessingFee { get; set; }

        [Display(Name = "Fixed Fee")]
        [Range(0, 999.99, ErrorMessage = "Fixed fee must be between 0 and 999.99")]
        [DataType(DataType.Currency)]
        public decimal FixedFee { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; }
    }
}
