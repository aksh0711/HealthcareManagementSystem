using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace HealthcareManagementSystem.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            
            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken))
            {
                TwilioClient.Init(accountSid, authToken);
            }
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var fromPhoneNumber = _configuration["Twilio:FromPhoneNumber"];
                
                if (string.IsNullOrEmpty(fromPhoneNumber))
                {
                    _logger.LogWarning("Twilio phone number not configured");
                    return false;
                }

                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(fromPhoneNumber),
                    to: new PhoneNumber(phoneNumber)
                );

                _logger.LogInformation($"SMS sent successfully. SID: {messageResource.Sid}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {phoneNumber}");
                return false;
            }
        }

        public async Task<bool> SendAppointmentReminderAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName)
        {
            var message = $"Dear {patientName}, this is a reminder that you have an appointment with {doctorName} on {appointmentDateTime:MMM dd, yyyy} at {appointmentDateTime:h:mm tt}. Please arrive 15 minutes early.";
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendAppointmentConfirmationAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName)
        {
            var message = $"Dear {patientName}, your appointment with {doctorName} has been confirmed for {appointmentDateTime:MMM dd, yyyy} at {appointmentDateTime:h:mm tt}. Thank you!";
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendAppointmentReminderSmsAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName)
        {
            // This is the same as SendAppointmentReminderAsync, using it to avoid duplication
            return await SendAppointmentReminderAsync(phoneNumber, patientName, appointmentDateTime, doctorName);
        }

        public async Task<bool> SendAppointmentConfirmationSmsAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName)
        {
            // This is the same as SendAppointmentConfirmationAsync, using it to avoid duplication
            return await SendAppointmentConfirmationAsync(phoneNumber, patientName, appointmentDateTime, doctorName);
        }

        public async Task<bool> SendPatientRegistrationWelcomeSmsAsync(string phoneNumber, string patientName, int patientId)
        {
            var message = $"Welcome to Healthcare Management System, {patientName}! Your registration has been completed successfully (Patient ID: {patientId}). We're here to provide you with the best care possible.";
            return await SendSmsAsync(phoneNumber, message);
        }
    }
}
