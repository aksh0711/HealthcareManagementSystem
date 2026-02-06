using HealthcareManagementSystem.Models;

namespace HealthcareManagementSystem.Services
{
    public interface IBillingService
    {
        // Invoice Operations
        Task<Invoice> CreateInvoiceAsync(int patientId, int? appointmentId = null, int? insuranceId = null);
        Task<Invoice?> GetInvoiceAsync(int invoiceId);
        Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<IEnumerable<Invoice>> GetInvoicesByPatientAsync(int patientId);
        Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
        Task<bool> UpdateInvoiceAsync(Invoice invoice);
        Task<bool> DeleteInvoiceAsync(int invoiceId);
        Task<string> GenerateInvoiceNumberAsync();

        // Invoice Item Operations
        Task<bool> AddInvoiceItemAsync(int invoiceId, string description, int quantity, decimal unitPrice);
        Task<bool> UpdateInvoiceItemAsync(int itemId, string description, int quantity, decimal unitPrice);
        Task<bool> DeleteInvoiceItemAsync(int itemId);

        // Payment Method Operations
        Task<IEnumerable<PaymentMethod>> GetActivePaymentMethodsAsync();
        Task<PaymentMethod?> GetPaymentMethodAsync(int paymentMethodId);
        Task<bool> CreatePaymentMethodAsync(PaymentMethod paymentMethod);

        // Payment Operations
        Task<Payment?> CreatePaymentAsync(int invoiceId, int paymentMethodId, decimal amount, PaymentType type = PaymentType.Full);
        Task<Payment?> GetPaymentAsync(int paymentId);
        Task<IEnumerable<Payment>> GetPaymentsByInvoiceAsync(int invoiceId);
        Task<bool> ProcessPaymentAsync(int paymentId);
        Task<bool> RefundPaymentAsync(int paymentId, decimal amount, string reason);

        // Gateway Operations
        Task<PaymentTransaction?> ProcessStripePaymentAsync(int paymentId, string tokenId);
        Task<PaymentTransaction?> ProcessRefundAsync(int paymentId, decimal amount);

        // Invoice Calculations
        Task<decimal> CalculateInvoiceTotalAsync(int invoiceId);
        Task<decimal> CalculateInsuranceCoverageAsync(int invoiceId);
        Task<bool> UpdateInvoiceStatusAsync(int invoiceId);

        // PDF Generation
        Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);

        // Statistics and Reporting
        Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetOutstandingBalanceAsync();
        Task<IEnumerable<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, decimal>> GetRevenueByPaymentMethodAsync(DateTime startDate, DateTime endDate);
    }
}
