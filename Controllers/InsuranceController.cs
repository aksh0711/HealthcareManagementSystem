using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HealthcareManagementSystem.Controllers
{
    public class InsuranceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<InsuranceController> _logger;

        public InsuranceController(
            ApplicationDbContext context, 
            IFileUploadService fileUploadService,
            ILogger<InsuranceController> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _logger = logger;
        }

        // GET: Insurance
        public async Task<IActionResult> Index(string searchString, string sortOrder, int? patientId, string status = "all")
        {
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.PatientFilter = patientId;
            ViewBag.StatusFilter = status;

            ViewBag.ProviderSortParm = sortOrder == "provider" ? "provider_desc" : "provider";
            ViewBag.PolicySortParm = sortOrder == "policy" ? "policy_desc" : "policy";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";

            var insurances = _context.Insurances
                .Include(i => i.Patient)
                .AsQueryable();

            // Filter by search string
            if (!string.IsNullOrEmpty(searchString))
            {
                insurances = insurances.Where(i => 
                    i.ProviderName.Contains(searchString) ||
                    i.PolicyNumber.Contains(searchString) ||
                    i.GroupNumber.Contains(searchString) ||
                    i.Patient.FirstName.Contains(searchString) ||
                    i.Patient.LastName.Contains(searchString));
            }

            // Filter by patient
            if (patientId.HasValue)
            {
                insurances = insurances.Where(i => i.PatientId == patientId.Value);
            }

            // Filter by status
            if (status != "all")
            {
                switch (status)
                {
                    case "active":
                        insurances = insurances.Where(i => i.IsActive && (!i.ExpirationDate.HasValue || i.ExpirationDate > DateTime.Today));
                        break;
                    case "expired":
                        insurances = insurances.Where(i => i.ExpirationDate.HasValue && i.ExpirationDate <= DateTime.Today);
                        break;
                    case "inactive":
                        insurances = insurances.Where(i => !i.IsActive);
                        break;
                }
            }

            // Apply sorting
            insurances = sortOrder switch
            {
                "provider" => insurances.OrderBy(i => i.ProviderName),
                "provider_desc" => insurances.OrderByDescending(i => i.ProviderName),
                "policy" => insurances.OrderBy(i => i.PolicyNumber),
                "policy_desc" => insurances.OrderByDescending(i => i.PolicyNumber),
                "date" => insurances.OrderBy(i => i.EffectiveDate),
                "date_desc" => insurances.OrderByDescending(i => i.EffectiveDate),
                "status" => insurances.OrderBy(i => i.IsActive).ThenBy(i => i.ExpirationDate),
                "status_desc" => insurances.OrderByDescending(i => i.IsActive).ThenByDescending(i => i.ExpirationDate),
                _ => insurances.OrderByDescending(i => i.CreatedAt)
            };

            // Load patients for filter dropdown
            ViewBag.Patients = new SelectList(
                await _context.Patients.OrderBy(p => p.FirstName).ToListAsync(),
                "PatientId",
                "FullName",
                patientId);

            return View(await insurances.ToListAsync());
        }

        // GET: Insurance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances
                .Include(i => i.Patient)
                .Include(i => i.Invoices)
                .FirstOrDefaultAsync(m => m.InsuranceId == id);

            if (insurance == null)
            {
                return NotFound();
            }

            return View(insurance);
        }

        // GET: Insurance/Create
        public async Task<IActionResult> Create(int? patientId)
        {
            ViewBag.PatientId = new SelectList(
                await _context.Patients.OrderBy(p => p.FirstName).ToListAsync(),
                "PatientId",
                "FullName",
                patientId);

            var model = new Insurance();
            if (patientId.HasValue)
            {
                model.PatientId = patientId.Value;
            }

            return View(model);
        }

        // POST: Insurance/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,ProviderName,PolicyNumber,GroupNumber,PrimaryInsuredName,RelationshipToPrimary,EffectiveDate,ExpirationDate,CopayAmount,DeductibleAmount,CoveragePercentage,Notes,IsActive")] Insurance insurance, IFormFile? frontCardImage, IFormFile? backCardImage, List<IFormFile>? additionalDocuments)
        {
            // Additional validation for PatientId
            if (insurance.PatientId <= 0)
            {
                ModelState.AddModelError(nameof(insurance.PatientId), "Please select a patient");
            }

            // Log validation errors for debugging
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>() });
                
                foreach (var error in errors)
                {
                    _logger.LogWarning("Validation error in field '{Field}': {Errors}", error.Field, string.Join(", ", error.Errors));
                }
            }
            else
            {
                try
                {
                    // Validate and handle file uploads
                    if (frontCardImage != null && frontCardImage.Length > 0)
                    {
                        if (!_fileUploadService.IsValidImageFile(frontCardImage))
                        {
                            ModelState.AddModelError("frontCardImage", "Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP).");
                        }
                        else
                        {
                            try
                            {
                                insurance.FrontCardImagePath = await _fileUploadService.UploadFileAsync(frontCardImage, "insurance");
                            }
                            catch (ArgumentException ex)
                            {
                                _logger.LogWarning(ex, "File validation error for front card image");
                                ModelState.AddModelError("frontCardImage", ex.Message);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error uploading front card image");
                                ModelState.AddModelError("frontCardImage", "Failed to upload front card image. Please try again.");
                            }
                        }
                    }

                    if (backCardImage != null && backCardImage.Length > 0)
                    {
                        if (!_fileUploadService.IsValidImageFile(backCardImage))
                        {
                            ModelState.AddModelError("backCardImage", "Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP).");
                        }
                        else
                        {
                            try
                            {
                                insurance.BackCardImagePath = await _fileUploadService.UploadFileAsync(backCardImage, "insurance");
                            }
                            catch (ArgumentException ex)
                            {
                                _logger.LogWarning(ex, "File validation error for back card image");
                                ModelState.AddModelError("backCardImage", ex.Message);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error uploading back card image");
                                ModelState.AddModelError("backCardImage", "Failed to upload back card image. Please try again.");
                            }
                        }
                    }

                    if (additionalDocuments != null && additionalDocuments.Count > 0)
                    {
                        var invalidFiles = additionalDocuments.Where(f => f.Length > 0 && !_fileUploadService.IsValidDocumentFile(f)).ToList();
                        if (invalidFiles.Any())
                        {
                            ModelState.AddModelError("additionalDocuments", "Please upload valid document files (PDF, DOC, DOCX, JPG, JPEG, PNG).");
                        }
                        else
                        {
                            try
                            {
                                var additionalPaths = new List<string>();
                                foreach (var file in additionalDocuments)
                                {
                                    if (file.Length > 0)
                                    {
                                        var path = await _fileUploadService.UploadFileAsync(file, "insurance");
                                        additionalPaths.Add(path);
                                    }
                                }
                                insurance.AdditionalDocumentsPath = string.Join(";", additionalPaths);
                            }
                            catch (ArgumentException ex)
                            {
                                _logger.LogWarning(ex, "File validation error for additional documents");
                                ModelState.AddModelError("additionalDocuments", ex.Message);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error uploading additional documents");
                                ModelState.AddModelError("additionalDocuments", "Failed to upload additional documents. Please try again.");
                            }
                        }
                    }

                    // Only proceed if no file upload errors occurred
                    if (ModelState.IsValid)
                    {
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            if (!string.IsNullOrEmpty(insurance.FrontCardImagePath) || 
                                !string.IsNullOrEmpty(insurance.BackCardImagePath) || 
                                !string.IsNullOrEmpty(insurance.AdditionalDocumentsPath))
                            {
                                insurance.DocumentsUploadedAt = DateTime.UtcNow;
                            }

                            insurance.CreatedAt = DateTime.UtcNow;
                            insurance.UpdatedAt = DateTime.UtcNow;

                            _context.Add(insurance);
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();

                            TempData["SuccessMessage"] = "Insurance policy created successfully!";
                            return RedirectToAction(nameof(Index));
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating insurance policy");
                    ModelState.AddModelError("", "An error occurred while creating the insurance policy. Please try again.");
                }
            }

            ViewBag.PatientId = new SelectList(
                await _context.Patients.OrderBy(p => p.FirstName).ToListAsync(),
                "PatientId",
                "FullName",
                insurance.PatientId);

            return View(insurance);
        }

        // GET: Insurance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances.FindAsync(id);
            if (insurance == null)
            {
                return NotFound();
            }

            ViewBag.PatientId = new SelectList(
                await _context.Patients.OrderBy(p => p.FirstName).ToListAsync(),
                "PatientId",
                "FullName",
                insurance.PatientId);

            return View(insurance);
        }

        // POST: Insurance/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InsuranceId,PatientId,ProviderName,PolicyNumber,GroupNumber,PrimaryInsuredName,RelationshipToPrimary,EffectiveDate,ExpirationDate,CopayAmount,DeductibleAmount,CoveragePercentage,Notes,IsActive")] Insurance insurance, IFormFile? frontCardImage, IFormFile? backCardImage, List<IFormFile>? additionalDocuments, bool removeFrontCard = false, bool removeBackCard = false, bool removeAdditionalDocs = false)
        {
            if (id != insurance.InsuranceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingInsurance = await _context.Insurances.AsNoTracking().FirstOrDefaultAsync(i => i.InsuranceId == id);
                    if (existingInsurance == null)
                    {
                        return NotFound();
                    }

                    // Handle file removals
                    if (removeFrontCard && !string.IsNullOrEmpty(existingInsurance.FrontCardImagePath))
                    {
                        _fileUploadService.DeleteFile(existingInsurance.FrontCardImagePath);
                        insurance.FrontCardImagePath = null;
                    }
                    else
                    {
                        insurance.FrontCardImagePath = existingInsurance.FrontCardImagePath;
                    }

                    if (removeBackCard && !string.IsNullOrEmpty(existingInsurance.BackCardImagePath))
                    {
                        _fileUploadService.DeleteFile(existingInsurance.BackCardImagePath);
                        insurance.BackCardImagePath = null;
                    }
                    else
                    {
                        insurance.BackCardImagePath = existingInsurance.BackCardImagePath;
                    }

                    if (removeAdditionalDocs && !string.IsNullOrEmpty(existingInsurance.AdditionalDocumentsPath))
                    {
                        var paths = existingInsurance.AdditionalDocumentsPath.Split(';');
                        foreach (var path in paths)
                        {
                            _fileUploadService.DeleteFile(path.Trim());
                        }
                        insurance.AdditionalDocumentsPath = null;
                    }
                    else
                    {
                        insurance.AdditionalDocumentsPath = existingInsurance.AdditionalDocumentsPath;
                    }

                    // Validate new file uploads before processing
                    if (frontCardImage != null && frontCardImage.Length > 0 && !_fileUploadService.IsValidImageFile(frontCardImage))
                    {
                        ModelState.AddModelError("frontCardImage", "Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP).");
                    }

                    if (backCardImage != null && backCardImage.Length > 0 && !_fileUploadService.IsValidImageFile(backCardImage))
                    {
                        ModelState.AddModelError("backCardImage", "Please upload a valid image file (JPG, JPEG, PNG, GIF, BMP).");
                    }

                    if (additionalDocuments != null && additionalDocuments.Count > 0)
                    {
                        var invalidFiles = additionalDocuments.Where(f => f.Length > 0 && !_fileUploadService.IsValidDocumentFile(f)).ToList();
                        if (invalidFiles.Any())
                        {
                            ModelState.AddModelError("additionalDocuments", "Please upload valid document files (PDF, DOC, DOCX, JPG, JPEG, PNG).");
                        }
                    }

                    // Only proceed if file validation passed
                    if (!ModelState.IsValid)
                    {
                        ViewBag.PatientId = new SelectList(
                            await _context.Patients.OrderBy(p => p.FirstName).ToListAsync(),
                            "PatientId",
                            "FullName",
                            insurance.PatientId);
                        return View(insurance);
                    }

                    // Handle new file uploads
                    bool documentsUpdated = false;

                    if (frontCardImage != null && frontCardImage.Length > 0)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(existingInsurance.FrontCardImagePath))
                            {
                                _fileUploadService.DeleteFile(existingInsurance.FrontCardImagePath);
                            }
                            insurance.FrontCardImagePath = await _fileUploadService.UploadFileAsync(frontCardImage, "insurance");
                            documentsUpdated = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading front card image during edit");
                            ModelState.AddModelError("frontCardImage", "Failed to upload front card image. Please try again.");
                        }
                    }

                    if (backCardImage != null && backCardImage.Length > 0)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(existingInsurance.BackCardImagePath))
                            {
                                _fileUploadService.DeleteFile(existingInsurance.BackCardImagePath);
                            }
                            insurance.BackCardImagePath = await _fileUploadService.UploadFileAsync(backCardImage, "insurance");
                            documentsUpdated = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading back card image during edit");
                            ModelState.AddModelError("backCardImage", "Failed to upload back card image. Please try again.");
                        }
                    }

                    if (additionalDocuments != null && additionalDocuments.Count > 0)
                    {
                        try
                        {
                            var additionalPaths = new List<string>();
                            
                            // Keep existing documents if not removing
                            if (!removeAdditionalDocs && !string.IsNullOrEmpty(existingInsurance.AdditionalDocumentsPath))
                            {
                                additionalPaths.AddRange(existingInsurance.AdditionalDocumentsPath.Split(';'));
                            }

                            // Add new documents
                            foreach (var file in additionalDocuments)
                            {
                                if (file.Length > 0)
                                {
                                    var path = await _fileUploadService.UploadFileAsync(file, "insurance");
                                    additionalPaths.Add(path);
                                }
                            }
                            
                            insurance.AdditionalDocumentsPath = string.Join(";", additionalPaths);
                            documentsUpdated = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error uploading additional documents during edit");
                            ModelState.AddModelError("additionalDocuments", "Failed to upload additional documents. Please try again.");
                        }
                    }

                    // Check if any file upload errors occurred
                    if (!ModelState.IsValid)
                    {
                        ViewBag.PatientId = new SelectList(
                            await _context.Patients.OrderBy(p => p.FirstName).ToListAsync(),
                            "PatientId",
                            "FullName",
                            insurance.PatientId);
                        return View(insurance);
                    }

                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        if (documentsUpdated)
                        {
                            insurance.DocumentsUploadedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            insurance.DocumentsUploadedAt = existingInsurance.DocumentsUploadedAt;
                        }

                        insurance.CreatedAt = existingInsurance.CreatedAt;
                        insurance.UpdatedAt = DateTime.UtcNow;

                        _context.Update(insurance);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        TempData["SuccessMessage"] = "Insurance policy updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InsuranceExists(insurance.InsuranceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating insurance policy");
                    ModelState.AddModelError("", "An error occurred while updating the insurance policy. Please try again.");
                }
            }

            ViewBag.PatientId = new SelectList(
                await _context.Patients.OrderBy(p => p.FirstName).ToListAsync(),
                "PatientId",
                "FullName",
                insurance.PatientId);

            return View(insurance);
        }

        // GET: Insurance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances
                .Include(i => i.Patient)
                .FirstOrDefaultAsync(m => m.InsuranceId == id);

            if (insurance == null)
            {
                return NotFound();
            }

            return View(insurance);
        }

        // POST: Insurance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var insurance = await _context.Insurances.FindAsync(id);
                if (insurance != null)
                {
                    // Delete associated files
                    if (!string.IsNullOrEmpty(insurance.FrontCardImagePath))
                    {
                        _fileUploadService.DeleteFile(insurance.FrontCardImagePath);
                    }

                    if (!string.IsNullOrEmpty(insurance.BackCardImagePath))
                    {
                        _fileUploadService.DeleteFile(insurance.BackCardImagePath);
                    }

                    if (!string.IsNullOrEmpty(insurance.AdditionalDocumentsPath))
                    {
                        var paths = insurance.AdditionalDocumentsPath.Split(';');
                        foreach (var path in paths)
                        {
                            _fileUploadService.DeleteFile(path.Trim());
                        }
                    }

                    _context.Insurances.Remove(insurance);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Insurance policy deleted successfully!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting insurance policy");
                TempData["ErrorMessage"] = "An error occurred while deleting the insurance policy. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Insurance/VerifyEligibility/5
        public async Task<IActionResult> VerifyEligibility(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insurance = await _context.Insurances
                .Include(i => i.Patient)
                .FirstOrDefaultAsync(i => i.InsuranceId == id);

            if (insurance == null)
            {
                return NotFound();
            }

            // In a real application, this would make an API call to verify eligibility
            // For now, we'll just return a mock response
            var verificationResult = new
            {
                IsEligible = insurance.IsActive && !insurance.IsExpired,
                EffectiveDate = insurance.EffectiveDate,
                ExpirationDate = insurance.ExpirationDate,
                CopayAmount = insurance.CopayAmount,
                DeductibleAmount = insurance.DeductibleAmount,
                CoveragePercentage = insurance.CoveragePercentage,
                VerifiedAt = DateTime.UtcNow
            };

            ViewBag.VerificationResult = verificationResult;
            return View(insurance);
        }

        private bool InsuranceExists(int id)
        {
            return _context.Insurances.Any(e => e.InsuranceId == id);
        }
    }
}
