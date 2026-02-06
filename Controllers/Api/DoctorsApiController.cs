using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.Utilities;

namespace HealthcareManagementSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Doctors
        /// <summary>
        /// Get all doctors
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Doctor>>> GetDoctors()
        {
            var doctors = await _context.Doctors
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();

            return Ok(doctors);
        }

        // GET: api/Doctors/5
        /// <summary>
        /// Get a specific doctor by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Doctor>> GetDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found" });
            }

            return Ok(doctor);
        }

        // GET: api/Doctors/{id}/appointments
        /// <summary>
        /// Get all appointments for a specific doctor
        /// </summary>
        [HttpGet("{id}/appointments")]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetDoctorAppointments(int id)
        {
            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                    .ThenInclude(a => a.Patient)
                .FirstOrDefaultAsync(d => d.DoctorId == id);

            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found" });
            }

            return Ok(doctor.Appointments.OrderByDescending(a => a.AppointmentDateTime));
        }

        // GET: api/Doctors/{id}/schedule?date=2024-01-01
        /// <summary>
        /// Get doctor's schedule for a specific date
        /// </summary>
        [HttpGet("{id}/schedule")]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetDoctorSchedule(int id, [FromQuery] DateTime date)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found" });
            }

            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == id && 
                           a.AppointmentDateTime >= startOfDay && 
                           a.AppointmentDateTime <= endOfDay)
                .OrderBy(a => a.AppointmentDateTime)
                .ToListAsync();

            return Ok(appointments);
        }

        // GET: api/Doctors/available?date=2024-01-01&time=10:00
        /// <summary>
        /// Get available doctors for a specific date and time
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Doctor>>> GetAvailableDoctors(
            [FromQuery] DateTime date, 
            [FromQuery] TimeSpan time)
        {
            var appointmentDateTime = date.Date.Add(time);
            
            // Get doctors who don't have appointments at this time
            var busyDoctorIds = await _context.Appointments
                .Where(a => a.AppointmentDateTime <= appointmentDateTime && 
                           a.AppointmentDateTime.AddMinutes(a.DurationMinutes) > appointmentDateTime &&
                           a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.DoctorId)
                .ToListAsync();

            var availableDoctors = await _context.Doctors
                .Where(d => d.IsAvailable && !busyDoctorIds.Contains(d.DoctorId))
                .OrderBy(d => d.LastName)
                .ToListAsync();

            return Ok(availableDoctors);
        }

        // GET: api/Doctors/search?term=cardiology
        /// <summary>
        /// Search doctors by name, specialization, or department
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Doctor>>> SearchDoctors([FromQuery] string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            var doctors = await _context.Doctors
                .Where(d => d.FirstName.Contains(term) ||
                           d.LastName.Contains(term) ||
                           d.Specialization.Contains(term) ||
                           d.Department.Contains(term) ||
                           d.Email.Contains(term))
                .OrderBy(d => d.LastName)
                .ThenBy(d => d.FirstName)
                .ToListAsync();

            return Ok(doctors);
        }

        // GET: api/Doctors/specializations
        /// <summary>
        /// Get all unique specializations
        /// </summary>
        [HttpGet("specializations")]
        public async Task<ActionResult<IEnumerable<string>>> GetSpecializations()
        {
            var specializations = await _context.Doctors
                .Where(d => !string.IsNullOrEmpty(d.Specialization))
                .Select(d => d.Specialization)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            return Ok(specializations);
        }

        // POST: api/Doctors
        /// <summary>
        /// Create a new doctor
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Doctor>> CreateDoctor(Doctor doctor)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Format phone number for consistent storage
                if (!string.IsNullOrWhiteSpace(doctor.PhoneNumber))
                {
                    doctor.PhoneNumber = PhoneNumberUtility.FormatForSms(doctor.PhoneNumber);
                }

                doctor.CreatedAt = DateTime.UtcNow;
                doctor.UpdatedAt = DateTime.UtcNow;

                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetDoctor), 
                    new { id = doctor.DoctorId }, 
                    doctor
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = $"Phone number format error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        // PUT: api/Doctors/5
        /// <summary>
        /// Update an existing doctor
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDoctor(int id, Doctor doctor)
        {
            if (id != doctor.DoctorId)
            {
                return BadRequest(new { message = "Doctor ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Format phone number for consistent storage
                if (!string.IsNullOrWhiteSpace(doctor.PhoneNumber))
                {
                    doctor.PhoneNumber = PhoneNumberUtility.FormatForSms(doctor.PhoneNumber);
                }

                doctor.UpdatedAt = DateTime.UtcNow;
                _context.Entry(doctor).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Doctor updated successfully", doctor });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = $"Phone number format error: {ex.Message}" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DoctorExists(id))
                {
                    return NotFound(new { message = "Doctor not found" });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        // PUT: api/Doctors/5/availability
        /// <summary>
        /// Update doctor availability status
        /// </summary>
        [HttpPut("{id}/availability")]
        public async Task<IActionResult> UpdateDoctorAvailability(int id, [FromBody] bool isAvailable)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found" });
            }

            doctor.IsAvailable = isAvailable;
            doctor.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Doctor availability updated to {(isAvailable ? "available" : "unavailable")}", doctor });
        }

        // DELETE: api/Doctors/5
        /// <summary>
        /// Delete a doctor
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null)
            {
                return NotFound(new { message = "Doctor not found" });
            }

            try
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Doctor deleted successfully" });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "Cannot delete doctor with existing appointments, medical records, or prescriptions." });
            }
        }

        private bool DoctorExists(int id)
        {
            return _context.Doctors.Any(e => e.DoctorId == id);
        }
    }
}
