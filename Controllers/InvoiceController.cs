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
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IBillingService _billingService;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(ApplicationDbContext context, IBillingService billingService, ILogger<InvoiceController> logger)
        {
            _context = context;
            _billingService = billingService;
            _logger = logger;
        }

        // GET: Invoice
        public async Task<IActionResult> Index(string searchString, InvoiceStatus? status, int page = 1, int pageSize = 10)
        {
            var query = _context.Invoices
                .Include(i => i.Patient)
                .Include(i => i.Appointment)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(i => i.InvoiceNumber.Contains(searchString) ||
                                       i.Patient.FirstName.Contains(searchString) ||
                                       i.Patient.LastName.Contains(searchString));
            }

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }

            var totalItems = await query.CountAsync();
            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new InvoiceIndexViewModel
            {
                Invoices = invoices,
                SearchString = searchString,
                SelectedStatus = status,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                TotalItems = totalItems,
                StatusList = Enum.GetValues<InvoiceStatus>()
                    .Select(s => new SelectListItem 
                    { 
                        Value = ((int)s).ToString(), 
                        Text = s.ToString() 
                    }).ToList()
            };

            return View(viewModel);
        }

        // GET: Invoice/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _billingService.GetInvoiceAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            var payments = await _billingService.GetPaymentsByInvoiceAsync(id);
            var paymentMethods = await _billingService.GetActivePaymentMethodsAsync();

            var viewModel = new InvoiceDetailsViewModel
            {
                Invoice = invoice,
                Payments = payments.ToList(),
                PaymentMethods = paymentMethods.Select(pm => new SelectListItem
                {
                    Value = pm.PaymentMethodId.ToString(),
                    Text = pm.Name
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Invoice/Create
        public async Task<IActionResult> Create(int? patientId, int? appointmentId)
        {
            await PopulateDropDownListsAsync();
            
            var viewModel = new InvoiceCreateViewModel
            {
                PatientId = patientId,
                AppointmentId = appointmentId,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30)
            };

            return View(viewModel);
        }

        // POST: Invoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (!viewModel.PatientId.HasValue)
                    {
                        ModelState.AddModelError(nameof(viewModel.PatientId), "Please select a patient.");
                        await PopulateDropDownListsAsync();
                        return View(viewModel);
                    }

                    var invoice = await _billingService.CreateInvoiceAsync(
                        viewModel.PatientId.Value, 
                        viewModel.AppointmentId, 
                        viewModel.InsuranceId);

                    invoice.DueDate = viewModel.DueDate;
                    invoice.Notes = viewModel.Notes ?? string.Empty;
                    invoice.TaxAmount = viewModel.TaxAmount;
                    invoice.DiscountAmount = viewModel.DiscountAmount;

                    await _billingService.UpdateInvoiceAsync(invoice);

                    TempData["SuccessMessage"] = "Invoice created successfully!";
                    return RedirectToAction(nameof(Edit), new { id = invoice.InvoiceId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating invoice");
                    ModelState.AddModelError("", "An error occurred while creating the invoice.");
                }
            }

            await PopulateDropDownListsAsync();
            return View(viewModel);
        }

        // GET: Invoice/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _billingService.GetInvoiceAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            await PopulateDropDownListsAsync();

            var viewModel = new InvoiceEditViewModel
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                PatientId = invoice.PatientId,
                AppointmentId = invoice.AppointmentId,
                InsuranceId = invoice.InsuranceId,
                InvoiceDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                TaxAmount = invoice.TaxAmount,
                DiscountAmount = invoice.DiscountAmount,
                Notes = invoice.Notes,
                Invoice = invoice,
                InvoiceItems = invoice.InvoiceItems.ToList()
            };

            return View(viewModel);
        }

        // POST: Invoice/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InvoiceEditViewModel viewModel)
        {
            if (id != viewModel.InvoiceId)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var invoice = await _billingService.GetInvoiceAsync(id);
                    if (invoice == null)
                    {
                        return NotFound();
                    }

                    invoice.AppointmentId = viewModel.AppointmentId;
                    invoice.InsuranceId = viewModel.InsuranceId;
                    invoice.DueDate = viewModel.DueDate;
                    invoice.Status = viewModel.Status;
                    invoice.TaxAmount = viewModel.TaxAmount;
                    invoice.DiscountAmount = viewModel.DiscountAmount;
                    invoice.Notes = viewModel.Notes ?? string.Empty;

                    await _billingService.UpdateInvoiceAsync(invoice);
                    await _billingService.CalculateInvoiceTotalAsync(id);
                    await _billingService.UpdateInvoiceStatusAsync(id);

                    TempData["SuccessMessage"] = "Invoice updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
                    ModelState.AddModelError("", "An error occurred while updating the invoice.");
                }
            }

            var invoice2 = await _billingService.GetInvoiceAsync(id);
            viewModel.Invoice = invoice2;
            viewModel.InvoiceItems = invoice2?.InvoiceItems.ToList() ?? new List<InvoiceItem>();
            await PopulateDropDownListsAsync();
            return View(viewModel);
        }

        // POST: Invoice/AddItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int invoiceId, string description, int quantity, decimal unitPrice)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(description) || quantity <= 0 || unitPrice <= 0)
                {
                    TempData["ErrorMessage"] = "Please provide valid item details.";
                    return RedirectToAction(nameof(Edit), new { id = invoiceId });
                }

                var success = await _billingService.AddInvoiceItemAsync(invoiceId, description, quantity, unitPrice);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Invoice item added successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to add invoice item.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding invoice item to invoice {InvoiceId}", invoiceId);
                TempData["ErrorMessage"] = "An error occurred while adding the invoice item.";
            }

            return RedirectToAction(nameof(Edit), new { id = invoiceId });
        }

        // POST: Invoice/UpdateItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(int itemId, int invoiceId, string description, int quantity, decimal unitPrice)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(description) || quantity <= 0 || unitPrice <= 0)
                {
                    TempData["ErrorMessage"] = "Please provide valid item details.";
                    return RedirectToAction(nameof(Edit), new { id = invoiceId });
                }

                var success = await _billingService.UpdateInvoiceItemAsync(itemId, description, quantity, unitPrice);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Invoice item updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update invoice item.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice item {ItemId}", itemId);
                TempData["ErrorMessage"] = "An error occurred while updating the invoice item.";
            }

            return RedirectToAction(nameof(Edit), new { id = invoiceId });
        }

        // POST: Invoice/DeleteItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(int itemId, int invoiceId)
        {
            try
            {
                var success = await _billingService.DeleteInvoiceItemAsync(itemId);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Invoice item deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete invoice item.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice item {ItemId}", itemId);
                TempData["ErrorMessage"] = "An error occurred while deleting the invoice item.";
            }

            return RedirectToAction(nameof(Edit), new { id = invoiceId });
        }

        // GET: Invoice/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _billingService.GetInvoiceAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Invoice/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _billingService.DeleteInvoiceAsync(id);
                
                if (success)
                {
                    TempData["SuccessMessage"] = "Invoice deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete invoice.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the invoice.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Invoice/DownloadPdf/5
        public async Task<IActionResult> DownloadPdf(int id)
        {
            try
            {
                var invoice = await _billingService.GetInvoiceAsync(id);
                if (invoice == null)
                {
                    return NotFound();
                }

                var pdfBytes = await _billingService.GenerateInvoicePdfAsync(id);
                var fileName = $"Invoice_{invoice.InvoiceNumber}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice {InvoiceId}", id);
                TempData["ErrorMessage"] = "An error occurred while generating the PDF.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Invoice/SendInvoice/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvoice(int id)
        {
            try
            {
                var invoice = await _billingService.GetInvoiceAsync(id);
                if (invoice == null)
                {
                    return NotFound();
                }

                if (invoice.Status == InvoiceStatus.Draft)
                {
                    invoice.Status = InvoiceStatus.Sent;
                    await _billingService.UpdateInvoiceAsync(invoice);
                }

                // Here you would typically integrate with an email service
                // For now, we'll just update the status and show a success message
                TempData["SuccessMessage"] = "Invoice sent successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invoice {InvoiceId}", id);
                TempData["ErrorMessage"] = "An error occurred while sending the invoice.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Invoice/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            var totalRevenue = await _billingService.GetTotalRevenueAsync(startDate, endDate);
            var outstandingBalance = await _billingService.GetOutstandingBalanceAsync();
            var overdueInvoices = await _billingService.GetOverdueInvoicesAsync();
            var recentInvoices = await _billingService.GetInvoicesByDateRangeAsync(startDate, endDate);
            var revenueByPaymentMethod = await _billingService.GetRevenueByPaymentMethodAsync(startDate, endDate);

            var viewModel = new InvoiceDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                OutstandingBalance = outstandingBalance,
                OverdueInvoicesCount = overdueInvoices.Count(),
                RecentInvoicesCount = recentInvoices.Count(),
                OverdueInvoices = overdueInvoices.Take(5).ToList(),
                RecentInvoices = recentInvoices.Take(10).ToList(),
                RevenueByPaymentMethod = revenueByPaymentMethod
            };

            return View(viewModel);
        }

        private async Task PopulateDropDownListsAsync()
        {
            try
            {
                ViewBag.Patients = new SelectList(
                    await _context.Patients
                        .OrderBy(p => p.LastName)
                        .ThenBy(p => p.FirstName)
                        .ToListAsync(),
                    "PatientId", "FullName");

                ViewBag.Appointments = new SelectList(
                    await _context.Appointments
                        .Include(a => a.Patient)
                        .Include(a => a.Doctor)
                        .Where(a => a.Status == AppointmentStatus.Completed)
                        .OrderByDescending(a => a.AppointmentDateTime)
                        .Take(100)
                        .ToListAsync(),
                    "AppointmentId", "DisplayText");

                ViewBag.Insurances = new SelectList(
                    await _context.Insurances
                        .Where(i => i.IsActive)
                        .OrderBy(i => i.ProviderName)
                        .ToListAsync(),
                    "InsuranceId", "ProviderName");

                ViewBag.InvoiceStatuses = new SelectList(
                    Enum.GetValues<InvoiceStatus>()
                        .Select(s => new { Value = (int)s, Text = s.ToString() }),
                    "Value", "Text");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdown lists");
                // Set empty lists to prevent null reference exceptions
                ViewBag.Patients = new SelectList(new List<object>(), "PatientId", "FullName");
                ViewBag.Appointments = new SelectList(new List<object>(), "AppointmentId", "DisplayText");
                ViewBag.Insurances = new SelectList(new List<object>(), "InsuranceId", "ProviderName");
                ViewBag.InvoiceStatuses = new SelectList(new List<object>(), "Value", "Text");
            }
        }
    }
}
