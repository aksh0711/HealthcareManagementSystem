using System.Threading.Tasks;

namespace HealthcareManagementSystem.Services
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendAppointmentReminderAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName);
        Task<bool> SendAppointmentConfirmationAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName);
        Task<bool> SendAppointmentReminderSmsAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName);
        Task<bool> SendAppointmentConfirmationSmsAsync(string phoneNumber, string patientName, DateTime appointmentDateTime, string doctorName);
        Task<bool> SendPatientRegistrationWelcomeSmsAsync(string phoneNumber, string patientName, int patientId);
    }
}
