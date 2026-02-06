using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;

namespace HealthcareManagementSystem.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrescriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Prescriptions
        /// <summary>
        /// Get all prescriptions with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPrescriptions(
            [FromQuery] int? patientId = null,
            [FromQuery] int? doctorId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? medicationName = null)
        {
            var query = _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .AsQueryable();

            if (patientId.HasValue)
                query = query.Where(p => p.PatientId == patientId);

            if (doctorId.HasValue)
                query = query.Where(p => p.DoctorId == doctorId);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive);

            if (!string.IsNullOrEmpty(medicationName))
                query = query.Where(p => p.MedicationName.Contains(medicationName));

            var prescriptions = await query
                .OrderByDescending(p => p.PrescribedDate)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.MedicationName,
                    p.Dosage,
                    p.Frequency,
                    p.DurationDays,
                    p.DurationDescription,
                    p.Instructions,
                    p.Quantity,
                    p.RefillsAllowed,
                    p.PrescribedDate,
                    p.StartDate,
                    p.EndDate,
                    p.IsActive,
                    p.IsExpired,
                    p.IsFulfilled,
                    Patient = new
                    {
                        p.Patient.PatientId,
                        p.Patient.FirstName,
                        p.Patient.LastName,
                        p.Patient.FullName,
                        p.Patient.DateOfBirth,
                        p.Patient.Age
                    },
                    Doctor = new
                    {
                        p.Doctor.DoctorId,
                        p.Doctor.FirstName,
                        p.Doctor.LastName,
                        p.Doctor.FullName,
                        p.Doctor.Specialization
                    }
                })
                .ToListAsync();

            return Ok(prescriptions);
        }

        // GET: api/Prescriptions/5
        /// <summary>
        /// Get a specific prescription by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPrescription(int id)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionFulfillments)
                .Where(p => p.PrescriptionId == id)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.MedicationName,
                    p.Dosage,
                    p.Frequency,
                    p.DurationDays,
                    p.DurationDescription,
                    p.Instructions,
                    p.SideEffects,
                    p.Quantity,
                    p.RefillsAllowed,
                    p.PrescribedDate,
                    p.StartDate,
                    p.EndDate,
                    p.IsActive,
                    p.IsExpired,
                    p.IsFulfilled,
                    p.CreatedAt,
                    p.UpdatedAt,
                    Patient = new
                    {
                        p.Patient.PatientId,
                        p.Patient.FirstName,
                        p.Patient.LastName,
                        p.Patient.FullName,
                        p.Patient.PhoneNumber,
                        p.Patient.Email,
                        p.Patient.DateOfBirth,
                        p.Patient.Age,
                        p.Patient.Allergies
                    },
                    Doctor = new
                    {
                        p.Doctor.DoctorId,
                        p.Doctor.FirstName,
                        p.Doctor.LastName,
                        p.Doctor.FullName,
                        p.Doctor.Specialization,
                        p.Doctor.LicenseNumber
                    },
                    Fulfillments = p.PrescriptionFulfillments.Select(f => new
                    {
                        f.FulfillmentId,
                        f.FulfilledDate,
                        f.PharmacyId,
                        f.QuantityDispensed
                    })
                })
                .FirstOrDefaultAsync();

            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            return Ok(prescription);
        }

        // GET: api/Prescriptions/active
        /// <summary>
        /// Get all active prescriptions
        /// </summary>
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<object>>> GetActivePrescriptions()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Where(p => p.IsActive && (!p.EndDate.HasValue || p.EndDate > DateTime.UtcNow))
                .OrderByDescending(p => p.PrescribedDate)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.MedicationName,
                    p.Dosage,
                    p.Frequency,
                    p.PrescribedDate,
                    p.EndDate,
                    Patient = new
                    {
                        p.Patient.PatientId,
                        p.Patient.FullName,
                        p.Patient.PhoneNumber
                    },
                    Doctor = new
                    {
                        p.Doctor.DoctorId,
                        p.Doctor.FullName,
                        p.Doctor.Specialization
                    }
                })
                .ToListAsync();

            return Ok(prescriptions);
        }

        // GET: api/Prescriptions/expired
        /// <summary>
        /// Get all expired prescriptions
        /// </summary>
        [HttpGet("expired")]
        public async Task<ActionResult<IEnumerable<object>>> GetExpiredPrescriptions()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Where(p => p.EndDate.HasValue && p.EndDate < DateTime.UtcNow)
                .OrderByDescending(p => p.EndDate)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.MedicationName,
                    p.Dosage,
                    p.PrescribedDate,
                    p.EndDate,
                    Patient = new
                    {
                        p.Patient.PatientId,
                        p.Patient.FullName,
                        p.Patient.PhoneNumber
                    },
                    Doctor = new
                    {
                        p.Doctor.DoctorId,
                        p.Doctor.FullName,
                        p.Doctor.Specialization
                    }
                })
                .ToListAsync();

            return Ok(prescriptions);
        }

        // GET: api/Prescriptions/search?medication=aspirin
        /// <summary>
        /// Search prescriptions by medication name
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchPrescriptions([FromQuery] string medication)
        {
            if (string.IsNullOrEmpty(medication))
            {
                return BadRequest(new { message = "Medication name is required" });
            }

            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .Where(p => p.MedicationName.Contains(medication))
                .OrderByDescending(p => p.PrescribedDate)
                .Select(p => new
                {
                    p.PrescriptionId,
                    p.MedicationName,
                    p.Dosage,
                    p.Frequency,
                    p.PrescribedDate,
                    p.IsActive,
                    Patient = new
                    {
                        p.Patient.PatientId,
                        p.Patient.FullName
                    },
                    Doctor = new
                    {
                        p.Doctor.DoctorId,
                        p.Doctor.FullName,
                        p.Doctor.Specialization
                    }
                })
                .ToListAsync();

            return Ok(prescriptions);
        }

        // GET: api/Prescriptions/statistics
        /// <summary>
        /// Get prescription statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetPrescriptionStatistics()
        {
            var today = DateTime.Today;
            var thisWeek = today.AddDays(-(int)today.DayOfWeek);
            var thisMonth = new DateTime(today.Year, today.Month, 1);

            var stats = new
            {
                TotalPrescriptions = await _context.Prescriptions.CountAsync(),
                ActivePrescriptions = await _context.Prescriptions
                    .CountAsync(p => p.IsActive && (!p.EndDate.HasValue || p.EndDate > DateTime.UtcNow)),
                ExpiredPrescriptions = await _context.Prescriptions
                    .CountAsync(p => p.EndDate.HasValue && p.EndDate < DateTime.UtcNow),
                TodayPrescriptions = await _context.Prescriptions
                    .CountAsync(p => p.PrescribedDate.Date == today),
                ThisWeekPrescriptions = await _context.Prescriptions
                    .CountAsync(p => p.PrescribedDate >= thisWeek),
                ThisMonthPrescriptions = await _context.Prescriptions
                    .CountAsync(p => p.PrescribedDate >= thisMonth),
                FulfilledPrescriptions = await _context.Prescriptions
                    .CountAsync(p => p.PrescriptionFulfillments.Any()),
                PendingFulfillment = await _context.Prescriptions
                    .CountAsync(p => p.IsActive && !p.PrescriptionFulfillments.Any())
            };

            return Ok(stats);
        }

        // GET: api/Prescriptions/medications
        /// <summary>
        /// Get all unique medication names
        /// </summary>
        [HttpGet("medications")]
        public async Task<ActionResult<IEnumerable<string>>> GetMedicationNames()
        {
            var medications = await _context.Prescriptions
                .Where(p => !string.IsNullOrEmpty(p.MedicationName))
                .Select(p => p.MedicationName)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();

            return Ok(medications);
        }

        // POST: api/Prescriptions
        /// <summary>
        /// Create a new prescription
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Prescription>> CreatePrescription(Prescription prescription)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check if patient exists
                var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == prescription.PatientId);
                if (!patientExists)
                {
                    return BadRequest(new { message = "Patient not found" });
                }

                // Check if doctor exists
                var doctorExists = await _context.Doctors.AnyAsync(d => d.DoctorId == prescription.DoctorId);
                if (!doctorExists)
                {
                    return BadRequest(new { message = "Doctor not found" });
                }

                // Set start and end dates if not provided
                if (!prescription.StartDate.HasValue)
                {
                    prescription.StartDate = prescription.PrescribedDate;
                }

                if (!prescription.EndDate.HasValue && prescription.DurationDays > 0)
                {
                    prescription.EndDate = prescription.StartDate.Value.AddDays(prescription.DurationDays);
                }

                prescription.CreatedAt = DateTime.UtcNow;
                prescription.UpdatedAt = DateTime.UtcNow;

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetPrescription),
                    new { id = prescription.PrescriptionId },
                    prescription
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        // PUT: api/Prescriptions/5
        /// <summary>
        /// Update an existing prescription
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePrescription(int id, Prescription prescription)
        {
            if (id != prescription.PrescriptionId)
            {
                return BadRequest(new { message = "Prescription ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Update end date if duration changed
                if (prescription.StartDate.HasValue && prescription.DurationDays > 0)
                {
                    prescription.EndDate = prescription.StartDate.Value.AddDays(prescription.DurationDays);
                }

                prescription.UpdatedAt = DateTime.UtcNow;
                _context.Entry(prescription).State = EntityState.Modified;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Prescription updated successfully", prescription });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PrescriptionExists(id))
                {
                    return NotFound(new { message = "Prescription not found" });
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

        // PUT: api/Prescriptions/5/deactivate
        /// <summary>
        /// Deactivate a prescription
        /// </summary>
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivatePrescription(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            prescription.IsActive = false;
            prescription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Prescription deactivated successfully", prescription });
        }

        // PUT: api/Prescriptions/5/activate
        /// <summary>
        /// Activate a prescription
        /// </summary>
        [HttpPut("{id}/activate")]
        public async Task<IActionResult> ActivatePrescription(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            prescription.IsActive = true;
            prescription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Prescription activated successfully", prescription });
        }

        // POST: api/Prescriptions/5/fulfill
        /// <summary>
        /// Record prescription fulfillment
        /// </summary>
        [HttpPost("{id}/fulfill")]
        public async Task<ActionResult<object>> FulfillPrescription(int id, [FromBody] PrescriptionFulfillment fulfillment)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            fulfillment.PrescriptionId = id;
            fulfillment.FulfilledDate = DateTime.UtcNow;

            _context.PrescriptionFulfillments.Add(fulfillment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Prescription fulfillment recorded successfully", fulfillment });
        }

        // DELETE: api/Prescriptions/5
        /// <summary>
        /// Delete a prescription
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrescription(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound(new { message = "Prescription not found" });
            }

            try
            {
                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Prescription deleted successfully" });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "Cannot delete prescription with existing fulfillments." });
            }
        }

        private bool PrescriptionExists(int id)
        {
            return _context.Prescriptions.Any(e => e.PrescriptionId == id);
        }
    }
}
