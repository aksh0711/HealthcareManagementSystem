using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;

namespace HealthcareManagementSystem.Controllers
{
    public class PrescriptionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Prescription
        public async Task<IActionResult> Index()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(prescriptions);
        }

        // GET: Prescription/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.PrescriptionId == id);

            if (prescription == null)
            {
                return NotFound();
            }

            return View(prescription);
        }

        // GET: Prescription/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName");
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName");
            return View();
        }

        // POST: Prescription/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,MedicationName,Dosage,Frequency,DurationDays,Instructions,SideEffects,Quantity,RefillsAllowed")] Prescription prescription)
        {
            // Additional validation for required fields
            if (prescription.PatientId <= 0)
            {
                ModelState.AddModelError("PatientId", "Please select a patient.");
            }
            
            if (prescription.DoctorId <= 0)
            {
                ModelState.AddModelError("DoctorId", "Please select a doctor.");
            }

            if (ModelState.IsValid)
            {
                prescription.IsActive = true;
                prescription.PrescribedDate = DateTime.UtcNow;
                prescription.StartDate = DateTime.UtcNow;
                prescription.EndDate = DateTime.UtcNow.AddDays(prescription.DurationDays);
                _context.Add(prescription);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Prescription created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName", prescription.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName", prescription.DoctorId);
            return View(prescription);
        }

        // GET: Prescription/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound();
            }

            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName", prescription.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName", prescription.DoctorId);
            return View(prescription);
        }

        // POST: Prescription/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PrescriptionId,PatientId,DoctorId,MedicationName,Dosage,Frequency,DurationDays,Instructions,Quantity,IsActive,PrescribedDate,CreatedAt")] Prescription prescription)
        {
            if (id != prescription.PrescriptionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    prescription.UpdatedAt = DateTime.UtcNow;
                    _context.Update(prescription);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Prescription updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PrescriptionExists(prescription.PrescriptionId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName", prescription.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName", prescription.DoctorId);
            return View(prescription);
        }

        // GET: Prescription/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.PrescriptionId == id);
            if (prescription == null)
            {
                return NotFound();
            }

            return View(prescription);
        }

        // POST: Prescription/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription != null)
            {
                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Prescription deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Prescription/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, bool isActive)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription != null)
            {
                prescription.IsActive = isActive;
                prescription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Prescription status updated to {(isActive ? "Active" : "Inactive")}.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PrescriptionExists(int id)
        {
            return _context.Prescriptions.Any(e => e.PrescriptionId == id);
        }

        // GET: Prescription/Patient/5
        public async Task<IActionResult> PatientPrescriptions(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Doctor)
                .Where(p => p.PatientId == id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewData["PatientName"] = $"{patient.FirstName} {patient.LastName}";
            ViewData["Title"] = $"Prescriptions for {patient.FirstName} {patient.LastName}";
            return View("Index", prescriptions);
        }
    }
}
