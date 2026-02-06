using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.Services;

namespace HealthcareManagementSystem.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AppointmentReminderService _reminderService;

        public AppointmentController(ApplicationDbContext context, AppointmentReminderService reminderService)
        {
            _context = context;
            _reminderService = reminderService;
        }

        // GET: Appointment
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();
            return View(appointments);
        }

        // GET: Appointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointment/Create
        public IActionResult Create()
        {
            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName");
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName");
            return View();
        }

        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,DoctorId,AppointmentDateTime,DurationMinutes,ReasonForVisit,Notes,Status,Fee,IsPaid")] Appointment appointment)
        {
            // Additional validation for required fields
            if (appointment.PatientId <= 0)
            {
                ModelState.AddModelError("PatientId", "Please select a patient.");
            }
            
            if (appointment.DoctorId <= 0)
            {
                ModelState.AddModelError("DoctorId", "Please select a doctor.");
            }
            
            if (!ModelState.IsValid)
            {
                // Reload the ViewData for dropdowns when validation fails
                ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName", appointment.PatientId);
                ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName", appointment.DoctorId);
                return View(appointment);
            }
            
            try
            {
                // Validate appointment date is in the future
                    if (appointment.AppointmentDateTime <= DateTime.Now)
                    {
                        ModelState.AddModelError("AppointmentDateTime", "Appointment date must be in the future.");
                    }
                    
                    // Validate that patient and doctor exist
                    var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == appointment.PatientId);
                    if (!patientExists)
                    {
                        ModelState.AddModelError("PatientId", "Selected patient does not exist.");
                    }
                    
                    var doctorExists = await _context.Doctors.AnyAsync(d => d.DoctorId == appointment.DoctorId);
                    if (!doctorExists)
                    {
                        ModelState.AddModelError("DoctorId", "Selected doctor does not exist.");
                    }
                    
                    // Check for conflicting appointments (same doctor, overlapping time)
                    var endTime = appointment.AppointmentDateTime.AddMinutes(appointment.DurationMinutes);
                    var conflictingAppointment = await _context.Appointments
                        .Where(a => a.DoctorId == appointment.DoctorId && 
                               a.Status != AppointmentStatus.Cancelled &&
                               a.AppointmentDateTime < endTime && 
                               (a.AppointmentDateTime.AddMinutes(a.DurationMinutes) > appointment.AppointmentDateTime))
                        .FirstOrDefaultAsync();
                        
                    if (conflictingAppointment != null)
                    {
                        ModelState.AddModelError("AppointmentDateTime", "This time slot conflicts with another appointment for the selected doctor.");
                    }
                    
                    if (ModelState.IsValid)
                    {
                        appointment.Status = AppointmentStatus.Scheduled;
                        appointment.CreatedAt = DateTime.UtcNow;
                        appointment.UpdatedAt = DateTime.UtcNow;
                        
                        _context.Add(appointment);
                        await _context.SaveChangesAsync();
                        
                        // Send confirmation and schedule reminders (with error handling)
                        try
                        {
                            await _reminderService.SendAppointmentConfirmation(appointment.AppointmentId);
                            _reminderService.ScheduleAppointmentReminders(appointment.AppointmentId);
                            TempData["SuccessMessage"] = "Appointment scheduled successfully. Confirmation sent and reminders scheduled.";
                        }
                        catch (Exception)
                        {
                            TempData["SuccessMessage"] = "Appointment scheduled successfully. Note: Confirmation email may not have been sent.";
                        }
                        
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while scheduling the appointment. Please try again.");
                }
            
            // Reload the ViewData for dropdowns when validation fails or errors occur
            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName", appointment.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName", appointment.DoctorId);
            return View(appointment);
        }

        // GET: Appointment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName", appointment.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName", appointment.DoctorId);
            return View(appointment);
        }

        // POST: Appointment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AppointmentId,PatientId,DoctorId,AppointmentDateTime,ReasonForVisit,Notes,Status,CreatedAt")] Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    appointment.UpdatedAt = DateTime.UtcNow;
                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Appointment updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.AppointmentId))
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

            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new { p.PatientId, FullName = p.FirstName + " " + p.LastName }), "PatientId", "FullName", appointment.PatientId);
            ViewData["DoctorId"] = new SelectList(_context.Doctors.Select(d => new { d.DoctorId, FullName = d.FirstName + " " + d.LastName + " (" + d.Specialization + ")" }), "DoctorId", "FullName", appointment.DoctorId);
            return View(appointment);
        }

        // GET: Appointment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.AppointmentId == id);
            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Appointment/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = status;
                appointment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Appointment status updated to {status}.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id);
        }

        // GET: Appointment/Today
        public async Task<IActionResult> Today()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDateTime >= today && a.AppointmentDateTime < tomorrow)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            ViewData["Title"] = "Today's Appointments";
            return View("Index", appointments);
        }
    }
}
