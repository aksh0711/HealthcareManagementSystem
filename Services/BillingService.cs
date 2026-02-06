using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Text;
using LocalInvoice = HealthcareManagementSystem.Models.Invoice;
using LocalPaymentMethod = HealthcareManagementSystem.Models.PaymentMethod;
using LocalInvoiceItem = HealthcareManagementSystem.Models.InvoiceItem;
using StripeInvoice = Stripe.Invoice;
using StripePaymentMethod = Stripe.PaymentMethod;
using StripeInvoiceItem = Stripe.InvoiceItem;

namespace HealthcareManagementSystem.Services
{
    public class BillingService : IBillingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BillingService> _logger;

        public BillingService(ApplicationDbContext context, IConfiguration configuration, ILogger<BillingService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            
            // Initialize Stripe
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        #region Invoice Operations

        public async Task<LocalInvoice> CreateInvoiceAsync(int patientId, int? appointmentId = null, int? insuranceId = null)
        {
            var invoice = new LocalInvoice
            {
                InvoiceNumber = await GenerateInvoiceNumberAsync(),
                PatientId = patientId,
                AppointmentId = appointmentId,
                InsuranceId = insuranceId,
                InvoiceDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Status = InvoiceStatus.Draft
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
        }

        public async Task<LocalInvoice?> GetInvoiceAsync(int invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .Include(i => i.Insurance)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                    .ThenInclude(p => p.PaymentMethod)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
        }

        public async Task<LocalInvoice?> GetInvoiceByNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .Include(i => i.Insurance)
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<IEnumerable<LocalInvoice>> GetInvoicesByPatientAsync(int patientId)
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.InvoiceItems)
                .Where(i => i.PatientId == patientId)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<LocalInvoice>> GetOverdueInvoicesAsync()
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                .Where(i => i.DueDate < DateTime.Today && 
                           i.Status != InvoiceStatus.Paid && 
                           i.Status != InvoiceStatus.Cancelled)
                .ToListAsync();
        }

        public async Task<bool> UpdateInvoiceAsync(LocalInvoice invoice)
        {
            try
            {
                invoice.UpdatedAt = DateTime.UtcNow;
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice {InvoiceId}", invoice.InvoiceId);
                return false;
            }
        }

        public async Task<bool> DeleteInvoiceAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(invoiceId);
                if (invoice != null)
                {
                    _context.Invoices.Remove(invoice);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var lastInvoice = await _context.Invoices
                .OrderByDescending(i => i.InvoiceId)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (lastInvoice != null && int.TryParse(lastInvoice.InvoiceNumber.Substring(3), out var current))
            {
                nextNumber = current + 1;
            }

            return $"INV{nextNumber:D6}";
        }

        #endregion

        #region Invoice Item Operations

        public async Task<bool> AddInvoiceItemAsync(int invoiceId, string description, int quantity, decimal unitPrice)
        {
            try
            {
                var item = new LocalInvoiceItem
                {
                    InvoiceId = invoiceId,
                    Description = description,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = quantity * unitPrice
                };

                _context.InvoiceItems.Add(item);
                await _context.SaveChangesAsync();

                // Update invoice total
                await CalculateInvoiceTotalAsync(invoiceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding invoice item to invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

        public async Task<bool> UpdateInvoiceItemAsync(int itemId, string description, int quantity, decimal unitPrice)
        {
            try
            {
                var item = await _context.InvoiceItems.FindAsync(itemId);
                if (item != null)
                {
                    item.Description = description;
                    item.Quantity = quantity;
                    item.UnitPrice = unitPrice;
                    item.TotalPrice = quantity * unitPrice;

                    await _context.SaveChangesAsync();
                    await CalculateInvoiceTotalAsync(item.InvoiceId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice item {ItemId}", itemId);
                return false;
            }
        }

        public async Task<bool> DeleteInvoiceItemAsync(int itemId)
        {
            try
            {
                var item = await _context.InvoiceItems.FindAsync(itemId);
                if (item != null)
                {
                    var invoiceId = item.InvoiceId;
                    _context.InvoiceItems.Remove(item);
                    await _context.SaveChangesAsync();
                    await CalculateInvoiceTotalAsync(invoiceId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice item {ItemId}", itemId);
                return false;
            }
        }

        #endregion

        #region Payment Method Operations

        public async Task<IEnumerable<LocalPaymentMethod>> GetActivePaymentMethodsAsync()
        {
            return await _context.PaymentMethods
                .Where(pm => pm.IsActive)
                .OrderBy(pm => pm.DisplayOrder)
                .ToListAsync();
        }

        public async Task<LocalPaymentMethod?> GetPaymentMethodAsync(int paymentMethodId)
        {
            return await _context.PaymentMethods.FindAsync(paymentMethodId);
        }

        public async Task<bool> CreatePaymentMethodAsync(LocalPaymentMethod paymentMethod)
        {
            try
            {
                _context.PaymentMethods.Add(paymentMethod);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment method");
                return false;
            }
        }

        #endregion

        #region Payment Operations

        public async Task<Payment?> CreatePaymentAsync(int invoiceId, int paymentMethodId, decimal amount, PaymentType type = PaymentType.Full)
        {
            try
            {
                var payment = new Payment
                {
                    InvoiceId = invoiceId,
                    PaymentMethodId = paymentMethodId,
                    PaymentNumber = await GeneratePaymentNumberAsync(),
                    Amount = amount,
                    Type = type,
                    Status = PaymentStatus.Pending,
                    PaymentDate = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for invoice {InvoiceId}", invoiceId);
                return null;
            }
        }

        public async Task<Payment?> GetPaymentAsync(int paymentId)
        {
            return await _context.Payments
                .Include(p => p.Invoice)
                .Include(p => p.PaymentMethod)
                .Include(p => p.PaymentTransactions)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByInvoiceAsync(int invoiceId)
        {
            return await _context.Payments
                .Include(p => p.PaymentMethod)
                .Where(p => p.InvoiceId == invoiceId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<bool> ProcessPaymentAsync(int paymentId)
        {
            try
            {
                var payment = await GetPaymentAsync(paymentId);
                if (payment == null) return false;

                payment.Status = PaymentStatus.Completed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.NetAmount = payment.Amount - payment.GatewayFee;

                // Update invoice
                var invoice = payment.Invoice;
                invoice.AmountPaid += payment.Amount;
                
                await UpdateInvoiceStatusAsync(payment.InvoiceId);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, decimal amount, string reason)
        {
            try
            {
                var payment = await GetPaymentAsync(paymentId);
                if (payment == null) return false;

                // Create refund payment
                var refundPayment = new Payment
                {
                    InvoiceId = payment.InvoiceId,
                    PaymentMethodId = payment.PaymentMethodId,
                    PaymentNumber = await GeneratePaymentNumberAsync(),
                    Amount = -amount,
                    Type = PaymentType.Refund,
                    Status = PaymentStatus.Completed,
                    PaymentDate = DateTime.UtcNow,
                    Notes = reason
                };

                _context.Payments.Add(refundPayment);

                // Update original payment status
                if (amount == payment.Amount)
                {
                    payment.Status = PaymentStatus.Refunded;
                }
                else
                {
                    payment.Status = PaymentStatus.PartiallyRefunded;
                }

                // Update invoice
                var invoice = payment.Invoice;
                invoice.AmountPaid -= amount;

                await UpdateInvoiceStatusAsync(payment.InvoiceId);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refunding payment {PaymentId}", paymentId);
                return false;
            }
        }

        private async Task<string> GeneratePaymentNumberAsync()
        {
            var lastPayment = await _context.Payments
                .OrderByDescending(p => p.PaymentId)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (lastPayment != null && int.TryParse(lastPayment.PaymentNumber.Substring(3), out var current))
            {
                nextNumber = current + 1;
            }

            return $"PAY{nextNumber:D6}";
        }

        #endregion

        #region Gateway Operations

        public async Task<PaymentTransaction?> ProcessStripePaymentAsync(int paymentId, string tokenId)
        {
            try
            {
                var payment = await GetPaymentAsync(paymentId);
                if (payment == null) return null;

                var service = new ChargeService();
                var options = new ChargeCreateOptions
                {
                    Amount = (long)(payment.Amount * 100), // Stripe uses cents
                    Currency = "usd",
                    Source = tokenId,
                    Description = $"Payment for Invoice {payment.Invoice.InvoiceNumber}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_id", payment.PaymentId.ToString() },
                        { "invoice_id", payment.InvoiceId.ToString() }
                    }
                };

                var charge = await service.CreateAsync(options);

                var transaction = new PaymentTransaction
                {
                    PaymentId = paymentId,
                    GatewayTransactionId = charge.Id,
                    Type = TransactionType.Charge,
                    Status = charge.Status == "succeeded" ? TransactionStatus.Captured : TransactionStatus.Failed,
                    Amount = payment.Amount,
                    Currency = "USD",
                    Gateway = "Stripe",
                    GatewayResponse = charge.ToString(),
                    GatewayFee = (charge.ApplicationFeeAmount ?? 0) / 100m,
                    NetAmount = payment.Amount - ((charge.ApplicationFeeAmount ?? 0) / 100m),
                    ProcessedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(transaction);

                // Update payment
                payment.Status = charge.Status == "succeeded" ? PaymentStatus.Completed : PaymentStatus.Failed;
                payment.GatewayTransactionId = charge.Id;
                payment.Gateway = "Stripe";
                payment.GatewayFee = transaction.GatewayFee;
                payment.NetAmount = transaction.NetAmount;
                payment.ProcessedAt = DateTime.UtcNow;

                if (charge.Status == "succeeded")
                {
                    await ProcessPaymentAsync(paymentId);
                }

                await _context.SaveChangesAsync();
                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment for payment {PaymentId}", paymentId);
                return null;
            }
        }

        public async Task<PaymentTransaction?> ProcessRefundAsync(int paymentId, decimal amount)
        {
            try
            {
                var payment = await GetPaymentAsync(paymentId);
                if (payment == null || string.IsNullOrEmpty(payment.GatewayTransactionId)) return null;

                var service = new RefundService();
                var options = new RefundCreateOptions
                {
                    Charge = payment.GatewayTransactionId,
                    Amount = (long)(amount * 100)
                };

                var refund = await service.CreateAsync(options);

                var transaction = new PaymentTransaction
                {
                    PaymentId = paymentId,
                    GatewayTransactionId = refund.Id,
                    Type = amount == payment.Amount ? TransactionType.Refund : TransactionType.PartialRefund,
                    Status = TransactionStatus.Captured,
                    Amount = amount,
                    Currency = "USD",
                    Gateway = "Stripe",
                    GatewayResponse = refund.ToString(),
                    ProcessedAt = DateTime.UtcNow
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                return transaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe refund for payment {PaymentId}", paymentId);
                return null;
            }
        }

        #endregion

        #region Invoice Calculations

        public async Task<decimal> CalculateInvoiceTotalAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice == null) return 0;

            invoice.SubTotal = invoice.InvoiceItems.Sum(item => item.TotalPrice);
            invoice.TotalAmount = invoice.SubTotal + invoice.TaxAmount - invoice.DiscountAmount - invoice.InsuranceCovered;

            await _context.SaveChangesAsync();
            return invoice.TotalAmount;
        }

        public async Task<decimal> CalculateInsuranceCoverageAsync(int invoiceId)
        {
            var invoice = await GetInvoiceAsync(invoiceId);
            if (invoice?.Insurance == null) return 0;

            // This would typically involve complex insurance calculation logic
            // For now, we'll use a simple percentage calculation
            var coveragePercentage = 0.8m; // 80% coverage example
            return invoice.SubTotal * coveragePercentage;
        }

        public async Task<bool> UpdateInvoiceStatusAsync(int invoiceId)
        {
            try
            {
                var invoice = await GetInvoiceAsync(invoiceId);
                if (invoice == null) return false;

                if (invoice.AmountPaid >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidDate = DateTime.UtcNow;
                }
                else if (invoice.AmountPaid > 0)
                {
                    invoice.Status = InvoiceStatus.PartiallyPaid;
                }
                else if (invoice.DueDate < DateTime.Today)
                {
                    invoice.Status = InvoiceStatus.Overdue;
                }
                else if (invoice.Status == InvoiceStatus.Draft)
                {
                    invoice.Status = InvoiceStatus.Sent;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice status for invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

        #endregion

        #region PDF Generation

        public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
        {
            var invoice = await GetInvoiceAsync(invoiceId);
            if (invoice == null) throw new ArgumentException("Invoice not found");

            using (var stream = new MemoryStream())
            {
                var document = new Document(PageSize.A4, 50, 50, 25, 25);
                var writer = PdfWriter.GetInstance(document, stream);
                document.Open();

                // Add invoice content
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 20);
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

                // Title
                document.Add(new Paragraph("HEALTHCARE INVOICE", titleFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph($"Invoice #{invoice.InvoiceNumber}", headerFont) { Alignment = Element.ALIGN_CENTER });
                document.Add(new Paragraph(" "));

                // Patient Information
                document.Add(new Paragraph($"Patient: {invoice.Patient.FullName}", normalFont));
                document.Add(new Paragraph($"Email: {invoice.Patient.Email}", normalFont));
                document.Add(new Paragraph($"Phone: {invoice.Patient.PhoneNumber}", normalFont));
                document.Add(new Paragraph(" "));

                // Invoice Details
                document.Add(new Paragraph($"Invoice Date: {invoice.InvoiceDate:MM/dd/yyyy}", normalFont));
                document.Add(new Paragraph($"Due Date: {invoice.DueDate:MM/dd/yyyy}", normalFont));
                document.Add(new Paragraph($"Status: {invoice.StatusDisplayName}", normalFont));
                document.Add(new Paragraph(" "));

                // Invoice Items Table
                var table = new PdfPTable(4);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 40, 15, 20, 25 });

                // Table Headers
                table.AddCell(new PdfPCell(new Phrase("Description", headerFont)) { HorizontalAlignment = Element.ALIGN_LEFT });
                table.AddCell(new PdfPCell(new Phrase("Quantity", headerFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase("Unit Price", headerFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase("Total", headerFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });

                // Table Data
                foreach (var item in invoice.InvoiceItems)
                {
                    table.AddCell(new PdfPCell(new Phrase(item.Description, normalFont)) { HorizontalAlignment = Element.ALIGN_LEFT });
                    table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), normalFont)) { HorizontalAlignment = Element.ALIGN_CENTER });
                    table.AddCell(new PdfPCell(new Phrase(item.UnitPrice.ToString("C"), normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                    table.AddCell(new PdfPCell(new Phrase(item.TotalPrice.ToString("C"), normalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT });
                }

                document.Add(table);
                document.Add(new Paragraph(" "));

                // Totals
                document.Add(new Paragraph($"Subtotal: {invoice.SubTotal:C}", normalFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Tax: {invoice.TaxAmount:C}", normalFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Discount: {invoice.DiscountAmount:C}", normalFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Insurance Coverage: {invoice.InsuranceCovered:C}", normalFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Total Amount: {invoice.TotalAmount:C}", headerFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Amount Paid: {invoice.AmountPaid:C}", normalFont) { Alignment = Element.ALIGN_RIGHT });
                document.Add(new Paragraph($"Balance Due: {invoice.BalanceAmount:C}", headerFont) { Alignment = Element.ALIGN_RIGHT });

                document.Close();
                return stream.ToArray();
            }
        }

        #endregion

        #region Statistics and Reporting

        public async Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Where(p => p.Status == PaymentStatus.Completed && 
                           p.PaymentDate >= startDate && 
                           p.PaymentDate <= endDate)
                .SumAsync(p => p.Amount);
        }

        public async Task<decimal> GetOutstandingBalanceAsync()
        {
            return await _context.Invoices
                .Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled)
                .SumAsync(i => i.TotalAmount - i.AmountPaid);
        }

        public async Task<IEnumerable<LocalInvoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Invoices
                .Include(i => i.Patient)
                .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetRevenueByPaymentMethodAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Include(p => p.PaymentMethod)
                .Where(p => p.Status == PaymentStatus.Completed && 
                           p.PaymentDate >= startDate && 
                           p.PaymentDate <= endDate)
                .GroupBy(p => p.PaymentMethod.Name)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.Amount));
        }

        #endregion
    }
}
