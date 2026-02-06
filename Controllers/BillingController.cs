using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.Services;
using HealthcareManagementSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace HealthcareManagementSystem.Controllers
{
    [Authorize]
    public class BillingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBillingService _billingService;
        private readonly ILogger<BillingController> _logger;

        public BillingController(ApplicationDbContext context, IBillingService billingService, ILogger<BillingController> logger)
        {
            _context = context;
            _billingService = billingService;
            _logger = logger;
        }

        // GET: Billing/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfYear = new DateTime(today.Year, 1, 1);

                var todayRevenue = await _billingService.GetTotalRevenueAsync(today, today.AddDays(1));
                var monthRevenue = await _billingService.GetTotalRevenueAsync(startOfMonth, today.AddDays(1));
                var yearRevenue = await _billingService.GetTotalRevenueAsync(startOfYear, today.AddDays(1));
                var outstandingBalance = await _billingService.GetOutstandingBalanceAsync();

                var pendingPayments = await _context.Payments
                    .CountAsync(p => p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing);

                var overdueInvoices = await _billingService.GetOverdueInvoicesAsync();
                
                var failedPayments = await _context.Payments
                    .CountAsync(p => p.Status == PaymentStatus.Failed);

                var recentInvoices = await _billingService.GetInvoicesByDateRangeAsync(today.AddDays(-7), today.AddDays(1));
                
                var recentPayments = await _context.Payments
                    .Include(p => p.Invoice)
                        .ThenInclude(i => i.Patient)
                    .Include(p => p.PaymentMethod)
                    .Where(p => p.PaymentDate >= today.AddDays(-7))
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(10)
                    .ToListAsync();

                // Revenue by month for the last 12 months
                var revenueByMonth = new Dictionary<string, decimal>();
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = today.AddMonths(-i).AddDays(1 - today.AddMonths(-i).Day);
                    var monthEnd = monthStart.AddMonths(1);
                    var monthRevenueStat = await _billingService.GetTotalRevenueAsync(monthStart, monthEnd);
                    revenueByMonth[monthStart.ToString("MMM yyyy")] = monthRevenueStat;
                }

                var revenueByPaymentMethod = await _billingService.GetRevenueByPaymentMethodAsync(startOfMonth, today.AddDays(1));

                var viewModel = new BillingDashboardViewModel
                {
                    TodayRevenue = todayRevenue,
                    MonthRevenue = monthRevenue,
                    YearRevenue = yearRevenue,
                    OutstandingBalance = outstandingBalance,
                    PendingPaymentsCount = pendingPayments,
                    OverdueInvoicesCount = overdueInvoices.Count(),
                    FailedPaymentsCount = failedPayments,
                    RecentInvoices = recentInvoices.Take(5).ToList(),
                    RecentPayments = recentPayments,
                    OverdueInvoices = overdueInvoices.Take(5).ToList(),
                    RevenueByMonth = revenueByMonth,
                    RevenueByPaymentMethod = revenueByPaymentMethod
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading billing dashboard");
                return View(new BillingDashboardViewModel());
            }
        }

        // GET: Billing/ProcessPayment/5
        public async Task<IActionResult> ProcessPayment(int invoiceId)
        {
            var invoice = await _billingService.GetInvoiceAsync(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var paymentMethods = await _billingService.GetActivePaymentMethodsAsync();

            var viewModel = new PaymentCreateViewModel
            {
                InvoiceId = invoiceId,
                Amount = invoice.BalanceAmount,
                Invoice = invoice,
                PaymentMethods = paymentMethods.Select(pm => new SelectListItem
                {
                    Value = pm.PaymentMethodId.ToString(),
                    Text = pm.Name
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Billing/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                await PopulatePaymentViewModel(viewModel);
                return View(viewModel);
            }

            try
            {
                var invoice = await _billingService.GetInvoiceAsync(viewModel.InvoiceId);
                if (invoice == null)
                {
                    ModelState.AddModelError("", "Invoice not found.");
                    await PopulatePaymentViewModel(viewModel);
                    return View(viewModel);
                }

                var paymentMethod = await _billingService.GetPaymentMethodAsync(viewModel.PaymentMethodId);
                if (paymentMethod == null)
                {
                    ModelState.AddModelError("", "Payment method not found.");
                    await PopulatePaymentViewModel(viewModel);
                    return View(viewModel);
                }

                // Create payment record
                var payment = await _billingService.CreatePaymentAsync(
                    viewModel.InvoiceId, 
                    viewModel.PaymentMethodId, 
                    viewModel.Amount, 
                    viewModel.Type);

                if (payment == null)
                {
                    ModelState.AddModelError("", "Failed to create payment record.");
                    await PopulatePaymentViewModel(viewModel);
                    return View(viewModel);
                }

                // Process payment based on method type
                bool success = false;

                if (paymentMethod.RequiresGateway && paymentMethod.Type == PaymentMethodType.CreditCard)
                {
                    // For Stripe payments, redirect to payment processing
                    return RedirectToAction(nameof(ProcessStripePayment), new { paymentId = payment.PaymentId });
                }
                else
                {
                    // For non-gateway payments (cash, check, etc.), mark as completed
                    success = await _billingService.ProcessPaymentAsync(payment.PaymentId);
                }

                if (success)
                {
                    TempData["SuccessMessage"] = $"Payment of {viewModel.Amount:C} processed successfully!";
                    return RedirectToAction("Details", "Invoice", new { id = viewModel.InvoiceId });
                }
                else
                {
                    ModelState.AddModelError("", "Payment processing failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for invoice {InvoiceId}", viewModel.InvoiceId);
                ModelState.AddModelError("", "An error occurred while processing the payment.");
            }

            await PopulatePaymentViewModel(viewModel);
            return View(viewModel);
        }

        // GET: Billing/ProcessStripePayment/5
        public async Task<IActionResult> ProcessStripePayment(int paymentId)
        {
            var payment = await _billingService.GetPaymentAsync(paymentId);
            if (payment == null)
            {
                return NotFound();
            }

            ViewBag.StripePublishableKey = HttpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Stripe:PublishableKey"];

            return View(payment);
        }

        // POST: Billing/ProcessStripePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessStripePayment(int paymentId, string stripeToken)
        {
            if (string.IsNullOrEmpty(stripeToken))
            {
                TempData["ErrorMessage"] = "Payment token is required.";
                return RedirectToAction(nameof(ProcessStripePayment), new { paymentId });
            }

            try
            {
                var transaction = await _billingService.ProcessStripePaymentAsync(paymentId, stripeToken);
                
                if (transaction != null && transaction.IsSuccessful)
                {
                    var payment = await _billingService.GetPaymentAsync(paymentId);
                    TempData["SuccessMessage"] = $"Payment of {payment?.Amount:C} processed successfully via Stripe!";
                    return RedirectToAction("Details", "Invoice", new { id = payment?.InvoiceId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment processing failed. Please check your card details and try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment {PaymentId}", paymentId);
                TempData["ErrorMessage"] = "An error occurred while processing your payment. Please try again.";
            }

            return RedirectToAction(nameof(ProcessStripePayment), new { paymentId });
        }

        // POST: Billing/RefundPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefundPayment(int paymentId, decimal amount, string reason)
        {
            try
            {
                var payment = await _billingService.GetPaymentAsync(paymentId);
                if (payment == null)
                {
                    return NotFound();
                }

                if (amount <= 0 || amount > payment.Amount)
                {
                    TempData["ErrorMessage"] = "Invalid refund amount.";
                    return RedirectToAction("Details", "Invoice", new { id = payment.InvoiceId });
                }

                var success = await _billingService.RefundPaymentAsync(paymentId, amount, reason);

                if (success)
                {
                    TempData["SuccessMessage"] = $"Refund of {amount:C} processed successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to process refund.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
                TempData["ErrorMessage"] = "An error occurred while processing the refund.";
            }

            var paymentInfo = await _billingService.GetPaymentAsync(paymentId);
            return RedirectToAction("Details", "Invoice", new { id = paymentInfo?.InvoiceId ?? 0 });
        }

        // GET: Billing/PaymentMethods
        public async Task<IActionResult> PaymentMethods()
        {
            var paymentMethods = await _context.PaymentMethods
                .OrderBy(pm => pm.DisplayOrder)
                .ToListAsync();

            return View(paymentMethods);
        }

        // GET: Billing/CreatePaymentMethod
        public IActionResult CreatePaymentMethod()
        {
            var viewModel = new PaymentMethodViewModel();
            PopulatePaymentMethodTypeDropDown();
            return View(viewModel);
        }

        // POST: Billing/CreatePaymentMethod
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentMethod(PaymentMethodViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                PopulatePaymentMethodTypeDropDown();
                return View(viewModel);
            }

            try
            {
                var paymentMethod = new PaymentMethod
                {
                    Name = viewModel.Name,
                    Type = viewModel.Type,
                    Description = viewModel.Description ?? string.Empty,
                    IsActive = viewModel.IsActive,
                    RequiresGateway = viewModel.RequiresGateway,
                    GatewayProvider = viewModel.GatewayProvider ?? string.Empty,
                    ProcessingFee = viewModel.ProcessingFee,
                    FixedFee = viewModel.FixedFee,
                    DisplayOrder = viewModel.DisplayOrder
                };

                var success = await _billingService.CreatePaymentMethodAsync(paymentMethod);

                if (success)
                {
                    TempData["SuccessMessage"] = "Payment method created successfully!";
                    return RedirectToAction(nameof(PaymentMethods));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to create payment method.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment method");
                ModelState.AddModelError("", "An error occurred while creating the payment method.");
            }

            PopulatePaymentMethodTypeDropDown();
            return View(viewModel);
        }

        // GET: Billing/Reports
        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today.AddDays(1);

            var invoices = await _billingService.GetInvoicesByDateRangeAsync(start, end);
            var totalRevenue = await _billingService.GetTotalRevenueAsync(start, end);
            var revenueByPaymentMethod = await _billingService.GetRevenueByPaymentMethodAsync(start, end);

            var payments = await _context.Payments
                .Include(p => p.Invoice)
                    .ThenInclude(i => i.Patient)
                .Include(p => p.PaymentMethod)
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && p.Status == PaymentStatus.Completed)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RevenueByPaymentMethod = revenueByPaymentMethod;
            ViewBag.Invoices = invoices;
            ViewBag.Payments = payments;

            return View();
        }

        private async Task PopulatePaymentViewModel(PaymentCreateViewModel viewModel)
        {
            var invoice = await _billingService.GetInvoiceAsync(viewModel.InvoiceId);
            var paymentMethods = await _billingService.GetActivePaymentMethodsAsync();

            viewModel.Invoice = invoice;
            viewModel.PaymentMethods = paymentMethods.Select(pm => new SelectListItem
            {
                Value = pm.PaymentMethodId.ToString(),
                Text = pm.Name
            }).ToList();
        }

        private void PopulatePaymentMethodTypeDropDown()
        {
            ViewBag.PaymentMethodTypes = new SelectList(
                Enum.GetValues<PaymentMethodType>()
                    .Select(t => new { Value = (int)t, Text = t.ToString() }),
                "Value", "Text");
        }
    }
}
