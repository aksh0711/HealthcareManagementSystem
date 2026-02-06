using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.ViewModels;

namespace HealthcareManagementSystem.Services
{
    public interface IEmergencyAlertService
    {
        // Emergency Alert Creation and Management
        Task<EmergencyAlert> CreateEmergencyAlertAsync(
            string title, 
            string message, 
            EmergencyAlertLevel level, 
            EmergencyAlertType type, 
            EmergencyAlertTarget targetAudience, 
            string? createdBy = null,
            List<int>? specificPatientIds = null,
            bool requiresReadConfirmation = false,
            string? notes = null,
            DateTime? scheduledTime = null);

        Task<EmergencyAlert> GetEmergencyAlertAsync(int alertId);
        Task<List<EmergencyAlert>> GetActiveEmergencyAlertsAsync();
        Task<List<EmergencyAlert>> GetEmergencyAlertHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
        Task<EmergencyAlert> UpdateEmergencyAlertAsync(int alertId, string title, string message, EmergencyAlertLevel level, string? notes = null);
        Task DeleteEmergencyAlertAsync(int alertId);

        // Alert Activation and Deactivation
        Task<EmergencyAlert> ActivateEmergencyAlertAsync(int alertId, string? authorizedBy = null);
        Task<EmergencyAlert> DeactivateEmergencyAlertAsync(int alertId, string? deactivatedBy = null, string? reason = null);
        Task<EmergencyAlert> ScheduleEmergencyAlertAsync(int alertId, DateTime scheduledTime);

        // Priority and Escalation Management
        Task<List<EmergencyAlert>> GetAlertsByPriorityAsync(EmergencyAlertLevel level);
        Task EscalateAlertAsync(int alertId, EmergencyAlertLevel newLevel, string? escalationReason = null);
        Task<List<EmergencyAlertEscalation>> GetAlertEscalationHistoryAsync(int alertId);
        Task ProcessEscalationRulesAsync();

        // Recipient Management
        Task<List<EmergencyAlertRecipient>> GetAlertRecipientsAsync(int alertId);
        Task AddRecipientsToAlertAsync(int alertId, List<int> patientIds);
        Task AddPhoneNumbersToAlertAsync(int alertId, List<string> phoneNumbers);
        Task RemoveRecipientFromAlertAsync(int alertId, int recipientId);

        // Alert Processing and Distribution
        Task ProcessEmergencyAlertAsync(int alertId);
        Task ProcessScheduledAlertsAsync();
        Task<AlertDistributionResult> DistributeAlertAsync(EmergencyAlert alert);
        Task<bool> SendAlertToRecipientAsync(EmergencyAlert alert, EmergencyAlertRecipient recipient);

        // Confirmation and Response Tracking
        Task<bool> ConfirmAlertReadAsync(int alertId, string phoneNumber, DateTime? confirmationTime = null);
        Task<List<EmergencyAlertRecipient>> GetUnconfirmedRecipientsAsync(int alertId);
        Task<double> GetAlertConfirmationRateAsync(int alertId);
        Task ProcessReadConfirmationTimeoutAsync(int alertId);

        // Automated Follow-up
        Task SendFollowUpRemindersAsync(int alertId);
        Task<EmergencyAlert> CreateFollowUpAlertAsync(int originalAlertId, string followUpMessage, TimeSpan delay);
        Task ScheduleAutomaticFollowUpsAsync(int alertId, List<TimeSpan> followUpIntervals);

        // Emergency Contact Lists
        Task<List<EmergencyContact>> GetEmergencyContactsAsync(EmergencyContactType type);
        Task<EmergencyContact> AddEmergencyContactAsync(string name, string phoneNumber, EmergencyContactType type, string? email = null, string? role = null);
        Task UpdateEmergencyContactAsync(int contactId, string name, string phoneNumber, string? email = null, string? role = null);
        Task RemoveEmergencyContactAsync(int contactId);
        Task<bool> NotifyEmergencyContactsAsync(EmergencyAlert alert);

        // Templates and Automation
        Task<List<EmergencyAlertTemplate>> GetEmergencyAlertTemplatesAsync(EmergencyAlertType? type = null);
        Task<EmergencyAlertTemplate> CreateAlertTemplateAsync(string name, string messageTemplate, EmergencyAlertLevel defaultLevel, EmergencyAlertType type);
        Task<EmergencyAlert> CreateAlertFromTemplateAsync(int templateId, Dictionary<string, string> variables, EmergencyAlertTarget target);

        // Compliance and Audit
        Task<List<EmergencyAlertAuditLog>> GetAlertAuditLogAsync(int alertId);
        Task LogEmergencyAlertActionAsync(int alertId, string action, string? performedBy = null, string? details = null);
        Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate);

        // Analytics and Reporting
        Task<EmergencyAlertAnalytics> GetEmergencyAlertAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<EmergencyAlertLevel, TimeSpan>> GetAverageResponseTimesByLevelAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AlertEffectivenessReport>> GetAlertEffectivenessReportAsync(DateTime? startDate = null, DateTime? endDate = null);

        // System Integration
        Task<bool> IntegrateWithHospitalSystemAsync(string systemType, Dictionary<string, object> configuration);
        Task<bool> TriggerAlertFromExternalSystemAsync(string externalId, Dictionary<string, object> alertData);
        Task<List<SystemIntegrationLog>> GetSystemIntegrationLogsAsync(DateTime? startDate = null, DateTime? endDate = null);

        // Testing and Simulation
        Task<EmergencyAlert> CreateTestAlertAsync(string message, List<string> testPhoneNumbers, string? createdBy = null);
        Task<AlertTestResult> RunAlertSystemTestAsync(EmergencyAlertLevel level);
        Task<bool> ValidateAlertSystemConfigurationAsync();
    }

    // Supporting Models
    public class EmergencyAlertEscalation
    {
        public int EscalationId { get; set; }
        public int EmergencyAlertId { get; set; }
        public EmergencyAlertLevel FromLevel { get; set; }
        public EmergencyAlertLevel ToLevel { get; set; }
        public string? EscalationReason { get; set; }
        public DateTime EscalatedDate { get; set; }
        public string? EscalatedBy { get; set; }
    }

    public class EmergencyContact
    {
        public int EmergencyContactId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public EmergencyContactType Type { get; set; }
        public string? Role { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
    }

    public class EmergencyAlertTemplate
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public EmergencyAlertLevel DefaultLevel { get; set; }
        public EmergencyAlertType Type { get; set; }
        public List<string> Variables { get; set; } = new List<string>();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
    }

    public class EmergencyAlertAuditLog
    {
        public int AuditLogId { get; set; }
        public int EmergencyAlertId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? PerformedBy { get; set; }
        public string? Details { get; set; }
        public DateTime ActionDate { get; set; }
        public string? IpAddress { get; set; }
    }

    public class AlertDistributionResult
    {
        public int TotalRecipients { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime DistributionStartTime { get; set; }
        public DateTime DistributionEndTime { get; set; }
        public TimeSpan TotalDistributionTime { get; set; }
    }

    public class EmergencyAlertAnalytics
    {
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public int CompletedAlerts { get; set; }
        public Dictionary<EmergencyAlertLevel, int> AlertsByLevel { get; set; } = new Dictionary<EmergencyAlertLevel, int>();
        public Dictionary<EmergencyAlertType, int> AlertsByType { get; set; } = new Dictionary<EmergencyAlertType, int>();
        public double AverageResponseTime { get; set; }
        public double OverallConfirmationRate { get; set; }
        public int TotalRecipientsReached { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class AlertEffectivenessReport
    {
        public int AlertId { get; set; }
        public string AlertTitle { get; set; } = string.Empty;
        public EmergencyAlertLevel Level { get; set; }
        public DateTime SentDate { get; set; }
        public int TotalRecipients { get; set; }
        public int ConfirmedRecipients { get; set; }
        public double ConfirmationRate { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public string EffectivenessRating { get; set; } = string.Empty;
    }

    public class ComplianceReport
    {
        public DateTime ReportPeriodStart { get; set; }
        public DateTime ReportPeriodEnd { get; set; }
        public int TotalEmergencyAlerts { get; set; }
        public int AlertsWithConfirmation { get; set; }
        public double ComplianceRate { get; set; }
        public List<string> ComplianceIssues { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    public class AlertTestResult
    {
        public bool TestPassed { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public List<string> TestResults { get; set; } = new List<string>();
        public List<string> Issues { get; set; } = new List<string>();
        public DateTime TestDate { get; set; }
    }

    public class SystemIntegrationLog
    {
        public int LogId { get; set; }
        public string SystemName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Data { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime LogDate { get; set; }
    }

    // Enums
    public enum EmergencyContactType
    {
        Medical = 1,
        Administrative,
        Security,
        Technical,
        External
    }
}
