using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.Utilities;
using HealthcareManagementSystem.Services;

namespace HealthcareManagementSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(ApplicationDbContext context, IEmailService emailService, ISmsService smsService, ILogger<PatientsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        // GET: api/Patients
        /// <summary>
        /// Get all patients
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
        {
            var patients = await _context.Patients
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            return Ok(patients);
        }

        // GET: api/Patients/5
        /// <summary>
        /// Get a specific patient by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            return Ok(patient);
        }

        // POST: api/Patients
        /// <summary>
        /// Create a new patient
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient(Patient patient)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

                patient.CreatedAt = DateTime.UtcNow;
                patient.UpdatedAt = DateTime.UtcNow;

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                // Send welcome notifications
                await SendPatientRegistrationNotificationsAsync(patient);

                return CreatedAtAction(
                    nameof(GetPatient), 
                    new { id = patient.PatientId }, 
                    patient
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

        // PUT: api/Patients/5
        /// <summary>
        /// Update an existing patient
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePatient(int id, Patient patient)
        {
            if (id != patient.PatientId)
            {
                return BadRequest(new { message = "Patient ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
                _context.Entry(patient).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Patient updated successfully", patient });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = $"Phone number format error: {ex.Message}" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(id))
                {
                    return NotFound(new { message = "Patient not found" });
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

        // DELETE: api/Patients/5
        /// <summary>
        /// Delete a patient
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            try
            {
                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Patient deleted successfully" });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "Cannot delete patient with existing appointments, medical records, or prescriptions." });
            }
        }

        // GET: api/Patients/search?term=searchTerm
        /// <summary>
        /// Search patients by name, email, or phone number
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Patient>>> SearchPatients([FromQuery] string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return BadRequest(new { message = "Search term is required" });
            }

            var patients = await _context.Patients
                .Where(p => p.FirstName.Contains(term) ||
                           p.LastName.Contains(term) ||
                           p.Email.Contains(term) ||
                           p.PhoneNumber.Contains(term))
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

            return Ok(patients);
        }

        // GET: api/Patients/{id}/appointments
        /// <summary>
        /// Get all appointments for a specific patient
        /// </summary>
        [HttpGet("{id}/appointments")]
        public async Task<ActionResult<IEnumerable<Appointment>>> GetPatientAppointments(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Appointments)
                    .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(p => p.PatientId == id);

            if (patient == null)
            {
                return NotFound(new { message = "Patient not found" });
            }

            return Ok(patient.Appointments.OrderByDescending(a => a.AppointmentDateTime));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.PatientId == id);
        }

        /// <summary>
        /// Send welcome notifications (email and SMS) to a newly registered patient
        /// </summary>
        private async Task SendPatientRegistrationNotificationsAsync(Patient patient)
        {
            var patientFullName = $"{patient.FirstName} {patient.LastName}";
            
            try
            {
                // Send welcome email if email is provided
                if (!string.IsNullOrWhiteSpace(patient.Email))
                {
                    await _emailService.SendPatientRegistrationWelcomeAsync(
                        patient.Email, 
                        patientFullName, 
                        patient.PatientId);
                    
                    _logger.LogInformation($"Welcome email sent to {patient.Email} for patient {patientFullName} (ID: {patient.PatientId})");
                }

                // Send welcome SMS if phone number is provided
                if (!string.IsNullOrWhiteSpace(patient.PhoneNumber))
                {
                    await _smsService.SendPatientRegistrationWelcomeSmsAsync(
                        patient.PhoneNumber, 
                        patientFullName, 
                        patient.PatientId);
                    
                    _logger.LogInformation($"Welcome SMS sent to {patient.PhoneNumber} for patient {patientFullName} (ID: {patient.PatientId})");
                }

                _logger.LogInformation($"Patient registration notifications completed for {patientFullName} (ID: {patient.PatientId})");
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the patient registration
                _logger.LogError(ex, $"Failed to send registration notifications for patient {patientFullName} (ID: {patient.PatientId}): {ex.Message}");
                
                // You could optionally queue this for retry later using Hangfire
                // BackgroundJob.Enqueue(() => RetryPatientRegistrationNotifications(patient.PatientId));
            }
        }
    }
}
