using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;

namespace HealthcareManagementSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Appointments
        /// <summary>
        /// Get all appointments with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAppointments(
            [FromQuery] int? patientId = null,
            [FromQuery] int? doctorId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] AppointmentStatus? status = null)
        {
            var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            if (patientId.HasValue)
                query = query.Where(a => a.PatientId == patientId);

            if (doctorId.HasValue)
                query = query.Where(a => a.DoctorId == doctorId);

            if (startDate.HasValue)
                query = query.Where(a => a.AppointmentDateTime >= startDate);

            if (endDate.HasValue)
                query = query.Where(a => a.AppointmentDateTime <= endDate);

            if (status.HasValue)
                query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.AppointmentDateTime,
                    a.DurationMinutes,
                    a.Status,
                    a.StatusDisplayName,
                    a.ReasonForVisit,
                    a.Notes,
                    a.Fee,
                    a.IsPaid,
                    Patient = new
                    {
                        a.Patient.PatientId,
                        a.Patient.FirstName,
                        a.Patient.LastName,
                        a.Patient.FullName,
                        a.Patient.PhoneNumber,
                        a.Patient.Email
                    },
                    Doctor = new
                    {
                        a.Doctor.DoctorId,
                        a.Doctor.FirstName,
                        a.Doctor.LastName,
                        a.Doctor.FullName,
                        a.Doctor.Specialization
                    }
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // GET: api/Appointments/5
        /// <summary>
        /// Get a specific appointment by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetAppointment(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentId == id)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.AppointmentDateTime,
                    a.EndDateTime,
                    a.DurationMinutes,
                    a.Status,
                    a.StatusDisplayName,
                    a.ReasonForVisit,
                    a.Notes,
                    a.Fee,
                    a.IsPaid,
                    a.CreatedAt,
                    a.UpdatedAt,
                    Patient = new
                    {
                        a.Patient.PatientId,
                        a.Patient.FirstName,
                        a.Patient.LastName,
                        a.Patient.FullName,
                        a.Patient.PhoneNumber,
                        a.Patient.Email,
                        a.Patient.DateOfBirth,
                        a.Patient.Age
                    },
                    Doctor = new
                    {
                        a.Doctor.DoctorId,
                        a.Doctor.FirstName,
                        a.Doctor.LastName,
                        a.Doctor.FullName,
                        a.Doctor.Specialization,
                        a.Doctor.Department,
                        a.Doctor.ConsultationFee
                    }
                })
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found" });
            }

            return Ok(appointment);
        }

        // GET: api/Appointments/today
        /// <summary>
        /// Get today's appointments
        /// </summary>
        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<object>>> GetTodaysAppointments()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDateTime >= today && a.AppointmentDateTime < tomorrow)
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.AppointmentDateTime,
                    a.Status,
                    a.StatusDisplayName,
                    a.ReasonForVisit,
                    Patient = new
                    {
                        a.Patient.PatientId,
                        a.Patient.FullName,
                        a.Patient.PhoneNumber
                    },
                    Doctor = new
                    {
                        a.Doctor.DoctorId,
                        a.Doctor.FullName,
                        a.Doctor.Specialization
                    }
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // GET: api/Appointments/upcoming
        /// <summary>
        /// Get upcoming appointments (next 7 days)
        /// </summary>
        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<object>>> GetUpcomingAppointments()
        {
            var now = DateTime.Now;
            var nextWeek = now.AddDays(7);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDateTime >= now && 
                           a.AppointmentDateTime <= nextWeek &&
                           a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.AppointmentDateTime,
                    a.Status,
                    a.ReasonForVisit,
                    Patient = new
                    {
                        a.Patient.PatientId,
                        a.Patient.FullName,
                        a.Patient.PhoneNumber
                    },
                    Doctor = new
                    {
                        a.Doctor.DoctorId,
                        a.Doctor.FullName,
                        a.Doctor.Specialization
                    }
                })
                .ToListAsync();

            return Ok(appointments);
        }

        // GET: api/Appointments/statistics
        /// <summary>
        /// Get appointment statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetAppointmentStatistics()
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new
            {
                TotalAppointments = await _context.Appointments.CountAsync(),
                TodayAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime.Date == today),
                ThisWeekAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime >= thisWeek),
                ThisMonthAppointments = await _context.Appointments
                    .CountAsync(a => a.AppointmentDateTime >= thisMonth),
                CompletedAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == AppointmentStatus.Completed),
                CancelledAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == AppointmentStatus.Cancelled),
                PendingAppointments = await _context.Appointments
                    .CountAsync(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed)
            };

            return Ok(stats);
        }

        // POST: api/Appointments
        /// <summary>
        /// Create a new appointment
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Appointment>> CreateAppointment(Appointment appointment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check if patient exists
                var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == appointment.PatientId);
                if (!patientExists)
                {
                    return BadRequest(new { message = "Patient not found" });
                }

                // Check if doctor exists
                var doctor = await _context.Doctors.FindAsync(appointment.DoctorId);
                if (doctor == null)
                {
                    return BadRequest(new { message = "Doctor not found" });
                }

                if (!doctor.IsAvailable)
                {
                    return BadRequest(new { message = "Doctor is not available" });
                }

                // Check for scheduling conflicts
                var conflictingAppointment = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == appointment.DoctorId &&
                                  a.AppointmentDateTime < appointment.AppointmentDateTime.AddMinutes(appointment.DurationMinutes) &&
                                  a.AppointmentDateTime.AddMinutes(a.DurationMinutes) > appointment.AppointmentDateTime &&
                                  a.Status != AppointmentStatus.Cancelled);

                if (conflictingAppointment)
                {
                    return BadRequest(new { message = "Doctor has a scheduling conflict at this time" });
                }

                // Set default fee from doctor's consultation fee if not provided
                if (appointment.Fee <= 0)
                {
                    appointment.Fee = doctor.ConsultationFee;
                }

                appointment.CreatedAt = DateTime.UtcNow;
                appointment.UpdatedAt = DateTime.UtcNow;

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetAppointment),
                    new { id = appointment.AppointmentId },
                    appointment
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        // PUT: api/Appointments/5
        /// <summary>
        /// Update an existing appointment
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAppointment(int id, Appointment appointment)
        {
            if (id != appointment.AppointmentId)
            {
                return BadRequest(new { message = "Appointment ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check for scheduling conflicts (excluding current appointment)
                var conflictingAppointment = await _context.Appointments
                    .AnyAsync(a => a.AppointmentId != id &&
                                  a.DoctorId == appointment.DoctorId &&
                                  a.AppointmentDateTime < appointment.AppointmentDateTime.AddMinutes(appointment.DurationMinutes) &&
                                  a.AppointmentDateTime.AddMinutes(a.DurationMinutes) > appointment.AppointmentDateTime &&
                                  a.Status != AppointmentStatus.Cancelled);

                if (conflictingAppointment)
                {
                    return BadRequest(new { message = "Doctor has a scheduling conflict at this time" });
                }

                appointment.UpdatedAt = DateTime.UtcNow;
                _context.Entry(appointment).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Appointment updated successfully", appointment });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(id))
                {
                    return NotFound(new { message = "Appointment not found" });
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

        // PUT: api/Appointments/5/status
        /// <summary>
        /// Update appointment status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found" });
            }

            appointment.Status = status;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Appointment status updated to {status}", appointment });
        }

        // PUT: api/Appointments/5/confirm
        /// <summary>
        /// Confirm an appointment
        /// </summary>
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found" });
            }

            appointment.Status = AppointmentStatus.Confirmed;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment confirmed successfully", appointment });
        }

        // PUT: api/Appointments/5/cancel
        /// <summary>
        /// Cancel an appointment
        /// </summary>
        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found" });
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Appointment cancelled successfully", appointment });
        }

        // DELETE: api/Appointments/5
        /// <summary>
        /// Delete an appointment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAppointment(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound(new { message = "Appointment not found" });
            }

            try
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Appointment deleted successfully" });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "Cannot delete appointment with existing medical records or lab tests." });
            }
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.AppointmentId == id);
        }
    }
}
