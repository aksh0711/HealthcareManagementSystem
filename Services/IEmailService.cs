namespace HealthcareManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = false);
        Task SendAppointmentReminderAsync(string patientEmail, string patientName, DateTime appointmentDateTime, string doctorName, string appointmentDetails);
        Task SendAppointmentConfirmationAsync(string patientEmail, string patientName, DateTime appointmentDateTime, string doctorName, string appointmentDetails);
        Task SendLabResultsNotificationAsync(string patientEmail, string patientName, string testName, string status);
        Task SendInvoiceAsync(string patientEmail, string patientName, string invoiceNumber, decimal amount, DateTime dueDate);
        Task SendPrescriptionReadyAsync(string patientEmail, string patientName, string pharmacyName, string prescriptionDetails);
        Task SendPatientRegistrationWelcomeAsync(string patientEmail, string patientName, int patientId);
    }
}
