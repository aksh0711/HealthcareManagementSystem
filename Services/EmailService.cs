using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace HealthcareManagementSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    _configuration["Email:FromName"] ?? "Healthcare Management System",
                    _configuration["Email:FromAddress"] ?? "noreply@healthcare.com"));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                // For development, we'll use a simple SMTP configuration
                // In production, configure proper SMTP settings in appsettings.json
                var smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
                var username = _configuration["Email:Username"];
                var password = _configuration["Email:Password"];

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    // Log the email instead of sending if no SMTP credentials are configured
                    _logger.LogInformation($"Email would be sent to {to} with subject '{subject}':\n{body}");
                    return;
                }

                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                throw;
            }
        }

        public async Task SendAppointmentReminderAsync(string patientEmail, string patientName, DateTime appointmentDateTime, string doctorName, string appointmentDetails)
        {
            var subject = "🏥 Appointment Reminder - Healthcare Management System";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #2E7D8B; color: white; padding: 20px; text-align: center;'>
                        <h1>🏥 Appointment Reminder</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <p>Dear <strong>{patientName}</strong>,</p>
                        
                        <p>This is a friendly reminder about your upcoming appointment:</p>
                        
                        <div style='background-color: white; padding: 20px; border-left: 4px solid #2E7D8B; margin: 20px 0;'>
                            <h3 style='color: #2E7D8B; margin-top: 0;'>📅 Appointment Details</h3>
                            <p><strong>Date & Time:</strong> {appointmentDateTime:dddd, MMMM d, yyyy 'at' h:mm tt}</p>
                            <p><strong>Doctor:</strong> Dr. {doctorName}</p>
                            <p><strong>Details:</strong> {appointmentDetails}</p>
                        </div>
                        
                        <div style='background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>📋 Please Note:</strong></p>
                            <ul style='margin: 10px 0;'>
                                <li>Arrive 15 minutes before your appointment</li>
                                <li>Bring a valid ID and insurance card</li>
                                <li>Bring any medications you're currently taking</li>
                                <li>If you need to reschedule, please call us at least 24 hours in advance</li>
                            </ul>
                        </div>
                        
                        <p>If you have any questions or need to reschedule, please contact our office.</p>
                        
                        <p style='margin-top: 30px;'>Best regards,<br>
                        <strong>Healthcare Management System Team</strong></p>
                    </div>
                    <div style='background-color: #2E7D8B; color: white; padding: 10px; text-align: center; font-size: 12px;'>
                        This is an automated reminder. Please do not reply to this email.
                    </div>
                </div>";

            await SendEmailAsync(patientEmail, subject, body, true);
        }

        public async Task SendAppointmentConfirmationAsync(string patientEmail, string patientName, DateTime appointmentDateTime, string doctorName, string appointmentDetails)
        {
            var subject = "✅ Appointment Confirmed - Healthcare Management System";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #28a745; color: white; padding: 20px; text-align: center;'>
                        <h1>✅ Appointment Confirmed</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <p>Dear <strong>{patientName}</strong>,</p>
                        
                        <p>Your appointment has been successfully scheduled!</p>
                        
                        <div style='background-color: white; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0;'>
                            <h3 style='color: #28a745; margin-top: 0;'>📅 Appointment Details</h3>
                            <p><strong>Date & Time:</strong> {appointmentDateTime:dddd, MMMM d, yyyy 'at' h:mm tt}</p>
                            <p><strong>Doctor:</strong> Dr. {doctorName}</p>
                            <p><strong>Details:</strong> {appointmentDetails}</p>
                        </div>
                        
                        <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>📋 What to Bring:</strong></p>
                            <ul style='margin: 10px 0;'>
                                <li>Valid photo ID</li>
                                <li>Insurance card and copay</li>
                                <li>List of current medications</li>
                                <li>Any relevant medical records</li>
                            </ul>
                        </div>
                        
                        <p>We'll send you a reminder closer to your appointment date.</p>
                        
                        <p style='margin-top: 30px;'>Thank you for choosing our healthcare services!</p>
                        
                        <p>Best regards,<br>
                        <strong>Healthcare Management System Team</strong></p>
                    </div>
                    <div style='background-color: #28a745; color: white; padding: 10px; text-align: center; font-size: 12px;'>
                        For questions or changes, contact our office during business hours.
                    </div>
                </div>";

            await SendEmailAsync(patientEmail, subject, body, true);
        }

        public async Task SendLabResultsNotificationAsync(string patientEmail, string patientName, string testName, string status)
        {
            var subject = "🧪 Lab Results Available - Healthcare Management System";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #17a2b8; color: white; padding: 20px; text-align: center;'>
                        <h1>🧪 Lab Results Available</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <p>Dear <strong>{patientName}</strong>,</p>
                        
                        <p>Your lab results for <strong>{testName}</strong> are now available.</p>
                        
                        <div style='background-color: white; padding: 20px; border-left: 4px solid #17a2b8; margin: 20px 0;'>
                            <h3 style='color: #17a2b8; margin-top: 0;'>📊 Test Information</h3>
                            <p><strong>Test:</strong> {testName}</p>
                            <p><strong>Status:</strong> {status}</p>
                        </div>
                        
                        <p>Please contact your doctor's office to discuss your results or schedule a follow-up appointment if needed.</p>
                        
                        <p style='margin-top: 30px;'>Best regards,<br>
                        <strong>Healthcare Management System Team</strong></p>
                    </div>
                </div>";

            await SendEmailAsync(patientEmail, subject, body, true);
        }

        public async Task SendInvoiceAsync(string patientEmail, string patientName, string invoiceNumber, decimal amount, DateTime dueDate)
        {
            var subject = "💳 Healthcare Invoice - Healthcare Management System";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #6c757d; color: white; padding: 20px; text-align: center;'>
                        <h1>💳 Invoice #{invoiceNumber}</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <p>Dear <strong>{patientName}</strong>,</p>
                        
                        <p>Please find your healthcare invoice details below:</p>
                        
                        <div style='background-color: white; padding: 20px; border-left: 4px solid #6c757d; margin: 20px 0;'>
                            <h3 style='color: #6c757d; margin-top: 0;'>💵 Invoice Details</h3>
                            <p><strong>Invoice Number:</strong> {invoiceNumber}</p>
                            <p><strong>Amount Due:</strong> ${amount:F2}</p>
                            <p><strong>Due Date:</strong> {dueDate:MMMM d, yyyy}</p>
                        </div>
                        
                        <p>Please ensure payment is made by the due date to avoid any late fees.</p>
                        
                        <p style='margin-top: 30px;'>Thank you,<br>
                        <strong>Healthcare Management System Team</strong></p>
                    </div>
                </div>";

            await SendEmailAsync(patientEmail, subject, body, true);
        }

        public async Task SendPrescriptionReadyAsync(string patientEmail, string patientName, string pharmacyName, string prescriptionDetails)
        {
            var subject = "💊 Prescription Ready for Pickup";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #fd7e14; color: white; padding: 20px; text-align: center;'>
                        <h1>💊 Prescription Ready</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <p>Dear <strong>{patientName}</strong>,</p>
                        
                        <p>Your prescription is ready for pickup at <strong>{pharmacyName}</strong>.</p>
                        
                        <div style='background-color: white; padding: 20px; border-left: 4px solid #fd7e14; margin: 20px 0;'>
                            <h3 style='color: #fd7e14; margin-top: 0;'>💊 Prescription Details</h3>
                            <p>{prescriptionDetails}</p>
                            <p><strong>Pickup Location:</strong> {pharmacyName}</p>
                        </div>
                        
                        <p>Please bring a valid ID when picking up your prescription.</p>
                        
                        <p style='margin-top: 30px;'>Best regards,<br>
                        <strong>Healthcare Management System Team</strong></p>
                    </div>
                </div>";

            await SendEmailAsync(patientEmail, subject, body, true);
        }

        public async Task SendPatientRegistrationWelcomeAsync(string patientEmail, string patientName, int patientId)
        {
            var subject = "🎉 Welcome to Healthcare Management System - Registration Successful";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <div style='background-color: #28a745; color: white; padding: 20px; text-align: center;'>
                        <h1>🎉 Welcome to Our Healthcare System!</h1>
                    </div>
                    <div style='padding: 30px; background-color: #f8f9fa;'>
                        <p>Dear <strong>{patientName}</strong>,</p>
                        
                        <p>Welcome to the Healthcare Management System! We're excited to have you as a new patient.</p>
                        
                        <div style='background-color: white; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0;'>
                            <h3 style='color: #28a745; margin-top: 0;'>🆔 Your Patient Information</h3>
                            <p><strong>Patient ID:</strong> {patientId}</p>
                            <p><strong>Name:</strong> {patientName}</p>
                            <p><strong>Email:</strong> {patientEmail}</p>
                        </div>
                        
                        <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>📋 What's Next?</strong></p>
                            <ul style='margin: 10px 0;'>
                                <li>Keep your Patient ID (#{patientId}) for future reference</li>
                                <li>You can now schedule appointments online or by calling our office</li>
                                <li>Update your medical history and insurance information in your profile</li>
                                <li>Download our mobile app for easy appointment management</li>
                            </ul>
                        </div>
                        
                        <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <p style='margin: 0;'><strong>📞 Contact Information:</strong></p>
                            <ul style='margin: 10px 0;'>
                                <li><strong>Phone:</strong> (555) 123-4567</li>
                                <li><strong>Email:</strong> support@healthcare.com</li>
                                <li><strong>Hours:</strong> Monday - Friday, 8:00 AM - 6:00 PM</li>
                            </ul>
                        </div>
                        
                        <p>If you have any questions or need assistance, our support team is here to help!</p>
                        
                        <p style='margin-top: 30px;'>Welcome aboard!</p>
                        
                        <p>Best regards,<br>
                        <strong>Healthcare Management System Team</strong></p>
                    </div>
                    <div style='background-color: #28a745; color: white; padding: 10px; text-align: center; font-size: 12px;'>
                        Thank you for choosing our healthcare services. We look forward to serving you!
                    </div>
                </div>";

            await SendEmailAsync(patientEmail, subject, body, true);
        }
    }
}
