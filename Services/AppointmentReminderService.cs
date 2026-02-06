using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace HealthcareManagementSystem.Services
{
    public class AppointmentReminderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(
            ApplicationDbContext context,
            IEmailService emailService,
            ISmsService smsService,
            ILogger<AppointmentReminderService> logger)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
            _logger = logger;
        }

        public void ScheduleAppointmentReminders(int appointmentId)
        {
            try
            {
                // Get the appointment to calculate reminder times
                var appointment = _context.Appointments.FirstOrDefault(a => a.AppointmentId == appointmentId);
                if (appointment == null)
                {
                    _logger.LogWarning($"Cannot schedule reminders - appointment {appointmentId} not found");
                    return;
                }
                
                var appointmentTime = appointment.AppointmentDateTime;
                var now = DateTime.Now;
                
                // Schedule 24-hour reminder (if appointment is more than 24 hours away)
                var reminder24Time = appointmentTime.AddHours(-24);
                if (reminder24Time > now)
                {
                    BackgroundJob.Schedule(() => Send24HourReminder(appointmentId), reminder24Time);
                }
                
                // Schedule 1-hour reminder (if appointment is more than 1 hour away)
                var reminder1Time = appointmentTime.AddHours(-1);
                if (reminder1Time > now)
                {
                    BackgroundJob.Schedule(() => Send1HourReminder(appointmentId), reminder1Time);
                }
                
                _logger.LogInformation($"Scheduled reminders for appointment {appointmentId} at {appointmentTime}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to schedule reminders for appointment {appointmentId}");
            }
        }

        public async Task Send24HourReminder(int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null || appointment.Status == AppointmentStatus.Cancelled)
                {
                    _logger.LogInformation($"Appointment {appointmentId} not found or cancelled, skipping 24-hour reminder");
                    return;
                }

                // Send email reminder
                await _emailService.SendAppointmentReminderAsync(
                    appointment.Patient.Email,
                    appointment.Patient.FullName,
                    appointment.AppointmentDateTime,
                    appointment.Doctor.FullName,
                    appointment.ReasonForVisit);

                // Send SMS reminder
                await _smsService.SendAppointmentReminderSmsAsync(
                    appointment.Patient.PhoneNumber,
                    appointment.Patient.FullName,
                    appointment.AppointmentDateTime,
                    appointment.Doctor.FullName);

                _logger.LogInformation($"24-hour reminder sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send 24-hour reminder for appointment {appointmentId}");
            }
        }

        public async Task Send1HourReminder(int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null || appointment.Status == AppointmentStatus.Cancelled)
                {
                    _logger.LogInformation($"Appointment {appointmentId} not found or cancelled, skipping 1-hour reminder");
                    return;
                }

                // Send SMS reminder only (shorter notice)
                var message = $"🏥 FINAL REMINDER\n\n" +
                             $"Hi {appointment.Patient.FullName}! Your appointment with Dr. {appointment.Doctor.FullName} " +
                             $"is in 1 hour at {appointment.AppointmentDateTime:h:mm tt}.\n\n" +
                             $"Please arrive 15 minutes early.\n\n" +
                             $"Healthcare Management System";

                await _smsService.SendSmsAsync(appointment.Patient.PhoneNumber, message);

                _logger.LogInformation($"1-hour reminder sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send 1-hour reminder for appointment {appointmentId}");
            }
        }

        public async Task SendAppointmentConfirmation(int appointmentId)
        {
            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                {
                    _logger.LogWarning($"Appointment {appointmentId} not found for confirmation");
                    return;
                }

                
                await _emailService.SendAppointmentConfirmationAsync(
                    appointment.Patient.Email,
                    appointment.Patient.FullName,
                    appointment.AppointmentDateTime,
                    appointment.Doctor.FullName,
                    appointment.ReasonForVisit);

                
                await _smsService.SendAppointmentConfirmationSmsAsync(
                    appointment.Patient.PhoneNumber,
                    appointment.Patient.FullName,
                    appointment.AppointmentDateTime,
                    appointment.Doctor.FullName);

                _logger.LogInformation($"Appointment confirmation sent for appointment {appointmentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send appointment confirmation for appointment {appointmentId}");
            }
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task ProcessDailyReminders()
        {
            try
            {
                var tomorrow = DateTime.Today.AddDays(1);
                var dayAfterTomorrow = tomorrow.AddDays(1);

                var upcomingAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .Where(a => a.AppointmentDateTime >= tomorrow && 
                               a.AppointmentDateTime < dayAfterTomorrow &&
                               a.Status != AppointmentStatus.Cancelled)
                    .ToListAsync();

                foreach (var appointment in upcomingAppointments)
                {
                    await Send24HourReminder(appointment.AppointmentId);
                }

                _logger.LogInformation($"Processed daily reminders for {upcomingAppointments.Count} appointments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process daily reminders");
            }
        }
    }
}
