using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.Utilities;

namespace HealthcareManagementSystem.Controllers
{
    // [Authorize] // Temporarily disabled for testing
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patient
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();
            return View(patients);
        }

        // GET: Patient/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(m => m.PatientId == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patient/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Patient/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,PhoneNumber,DateOfBirth,Gender,Address,EmergencyContactName,EmergencyContactPhone,BloodType,Allergies,MedicalHistory")] Patient patient)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Format phone numbers for consistent storage
                    if (!string.IsNullOrWhiteSpace(patient.PhoneNumber))
                    {
                        patient.PhoneNumber = PhoneNumberUtility.FormatForSms(patient.PhoneNumber);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(patient.EmergencyContactPhone))
                    {
                        patient.EmergencyContactPhone = PhoneNumberUtility.FormatForSms(patient.EmergencyContactPhone);
                    }

                    _context.Add(patient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Patient created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", $"Phone number format error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"An error occurred while creating the patient: {ex.Message}");
                }
            }
            return View(patient);
        }

        // GET: Patient/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }
            return View(patient);
        }

        // POST: Patient/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PatientId,FirstName,LastName,Email,PhoneNumber,DateOfBirth,Gender,Address,EmergencyContactName,EmergencyContactPhone,BloodType,Allergies,MedicalHistory,CreatedAt")] Patient patient)
        {
            if (id != patient.PatientId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Format phone numbers for consistent storage
                    if (!string.IsNullOrWhiteSpace(patient.PhoneNumber))
                    {
                        patient.PhoneNumber = PhoneNumberUtility.FormatForSms(patient.PhoneNumber);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(patient.EmergencyContactPhone))
                    {
                        patient.EmergencyContactPhone = PhoneNumberUtility.FormatForSms(patient.EmergencyContactPhone);
                    }

                    patient.UpdatedAt = DateTime.UtcNow;
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Patient updated successfully.";
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", $"Phone number format error: {ex.Message}");
                    return View(patient);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.PatientId))
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
                    ModelState.AddModelError("", $"An error occurred while updating the patient: {ex.Message}");
                    return View(patient);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // GET: Patient/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(m => m.PatientId == id);
            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // POST: Patient/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                try
                {
                    _context.Patients.Remove(patient);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Patient deleted successfully.";
                }
                catch (DbUpdateException)
                {
                    TempData["ErrorMessage"] = "Cannot delete patient with existing appointments, medical records, or prescriptions.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }

        // GET: Patient/Search
        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToAction(nameof(Index));
            }

            var patients = await _context.Patients
                .Where(p => p.FirstName.Contains(searchTerm) ||
                           p.LastName.Contains(searchTerm) ||
                           p.Email.Contains(searchTerm) ||
                           p.PhoneNumber.Contains(searchTerm))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            ViewData["SearchTerm"] = searchTerm;
            return View("Index", patients);
        }
    }
}
