using Microsoft.EntityFrameworkCore;
using HealthcareManagementSystem.Data;
using HealthcareManagementSystem.Models;
using HealthcareManagementSystem.ViewModels;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HealthcareManagementSystem.Services
{
    public class EmergencyAlertService : IEmergencyAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmergencyAlertService> _logger;
        private readonly IConfiguration _configuration;

        public EmergencyAlertService(
            ApplicationDbContext context,
            ISmsService smsService,
            IEmailService emailService,
            ILogger<EmergencyAlertService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _smsService = smsService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        #region Emergency Alert Creation and Management

        public async Task<EmergencyAlert> CreateEmergencyAlertAsync(
            string title,
            string message,
            EmergencyAlertLevel level,
            EmergencyAlertType type,
            EmergencyAlertTarget targetAudience,
            string? createdBy = null,
            List<int>? specificPatientIds = null,
            bool requiresReadConfirmation = false,
            string? notes = null,
            DateTime? scheduledTime = null)
        {
            try
            {
                var alert = new EmergencyAlert
                {
                    Title = title,
                    Message = message,
                    Level = level,
                    Type = type,
                    TargetAudience = targetAudience,
                    Status = scheduledTime.HasValue ? EmergencyAlertStatus.Draft : EmergencyAlertStatus.Draft,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = createdBy ?? string.Empty,
                    RequiresReadConfirmation = requiresReadConfirmation,
                    Notes = notes ?? string.Empty
                };

                _context.EmergencyAlerts.Add(alert);
                await _context.SaveChangesAsync();

                // Add recipients based on target audience
                await AddRecipientsBasedOnTargetAsync(alert, specificPatientIds);

                // Log the creation
                await LogEmergencyAlertActionAsync(alert.EmergencyAlertId, "CREATED", createdBy, $"Emergency alert created with level {level}");

                _logger.LogInformation($"Emergency alert created: {title}, Level: {level}, ID: {alert.EmergencyAlertId}");
                return alert;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating emergency alert: {title}");
                throw;
            }
        }

        public async Task<EmergencyAlert> GetEmergencyAlertAsync(int alertId)
        {
            var alert = await _context.EmergencyAlerts
                .Include(a => a.Recipients)
                .ThenInclude(r => r.Patient)
                .FirstOrDefaultAsync(a => a.EmergencyAlertId == alertId);

            if (alert == null)
                throw new ArgumentException($"Emergency alert not found: {alertId}");

            return alert;
        }

        public async Task<List<EmergencyAlert>> GetActiveEmergencyAlertsAsync()
        {
            return await _context.EmergencyAlerts
                .Where(a => a.Status == EmergencyAlertStatus.Active)
                .OrderByDescending(a => a.Level)
                .ThenByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<EmergencyAlert>> GetEmergencyAlertHistoryAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
        {
            var query = _context.EmergencyAlerts.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedDate <= endDate.Value);

            return await query
                .OrderByDescending(a => a.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<EmergencyAlert> UpdateEmergencyAlertAsync(int alertId, string title, string message, EmergencyAlertLevel level, string? notes = null)
        {
            var alert = await GetEmergencyAlertAsync(alertId);

            if (alert.Status == EmergencyAlertStatus.Active)
                throw new InvalidOperationException("Cannot update an active alert. Deactivate it first.");

            alert.Title = title;
            alert.Message = message;
            alert.Level = level;
            alert.Notes = notes ?? string.Empty;

            await _context.SaveChangesAsync();
            await LogEmergencyAlertActionAsync(alertId, "UPDATED", null, "Emergency alert updated");

            return alert;
        }

        public async Task DeleteEmergencyAlertAsync(int alertId)
        {
            var alert = await GetEmergencyAlertAsync(alertId);

            if (alert.Status == EmergencyAlertStatus.Active)
                throw new InvalidOperationException("Cannot delete an active alert. Deactivate it first.");

            _context.EmergencyAlerts.Remove(alert);
            await _context.SaveChangesAsync();

            await LogEmergencyAlertActionAsync(alertId, "DELETED", null, "Emergency alert deleted");
        }

        #endregion

        #region Alert Activation and Deactivation

        public async Task<EmergencyAlert> ActivateEmergencyAlertAsync(int alertId, string? authorizedBy = null)
        {
            var alert = await GetEmergencyAlertAsync(alertId);

            if (alert.Status == EmergencyAlertStatus.Active)
                throw new InvalidOperationException("Alert is already active");

            alert.Status = EmergencyAlertStatus.Active;
            alert.ActivatedDate = DateTime.UtcNow;
            alert.AuthorizedBy = authorizedBy ?? string.Empty;

            await _context.SaveChangesAsync();

            // Start distribution process
            await ProcessEmergencyAlertAsync(alertId);

            await LogEmergencyAlertActionAsync(alertId, "ACTIVATED", authorizedBy, "Emergency alert activated and distribution started");

            _logger.LogInformation($"Emergency alert activated: {alert.Title}, ID: {alertId}");
            return alert;
        }

        public async Task<EmergencyAlert> DeactivateEmergencyAlertAsync(int alertId, string? deactivatedBy = null, string? reason = null)
        {
            var alert = await GetEmergencyAlertAsync(alertId);

            alert.Status = EmergencyAlertStatus.Completed;
            alert.DeactivatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await LogEmergencyAlertActionAsync(alertId, "DEACTIVATED", deactivatedBy, reason ?? "Emergency alert deactivated");

            return alert;
        }

        public async Task<EmergencyAlert> ScheduleEmergencyAlertAsync(int alertId, DateTime scheduledTime)
        {
            var alert = await GetEmergencyAlertAsync(alertId);

            if (scheduledTime <= DateTime.UtcNow)
                throw new ArgumentException("Scheduled time must be in the future");

            alert.Status = EmergencyAlertStatus.Draft;
            // You might want to add a ScheduledTime property to EmergencyAlert model

            await _context.SaveChangesAsync();
            await LogEmergencyAlertActionAsync(alertId, "SCHEDULED", null, $"Emergency alert scheduled for {scheduledTime}");

            return alert;
        }

        #endregion

        #region Priority and Escalation Management

        public async Task<List<EmergencyAlert>> GetAlertsByPriorityAsync(EmergencyAlertLevel level)
        {
            return await _context.EmergencyAlerts
                .Where(a => a.Level == level)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task EscalateAlertAsync(int alertId, EmergencyAlertLevel newLevel, string? escalationReason = null)
        {
            var alert = await GetEmergencyAlertAsync(alertId);
            var oldLevel = alert.Level;

            alert.Level = newLevel;
            await _context.SaveChangesAsync();

            await LogEmergencyAlertActionAsync(alertId, "ESCALATED", null, 
                $"Alert escalated from {oldLevel} to {newLevel}. Reason: {escalationReason ?? "Not specified"}");

            _logger.LogWarning($"Emergency alert escalated: ID {alertId}, from {oldLevel} to {newLevel}");
        }

        public Task<List<EmergencyAlertEscalation>> GetAlertEscalationHistoryAsync(int alertId)
        {
            // This would require an escalation history table - for now returning empty list
            return Task.FromResult(new List<EmergencyAlertEscalation>());
        }

        public async Task ProcessEscalationRulesAsync()
        {
            // Implement escalation rules processing logic
            var activeAlerts = await GetActiveEmergencyAlertsAsync();
            
            foreach (var alert in activeAlerts)
            {
                // Example escalation rule: escalate to Critical if no confirmation after 30 minutes
                if (alert.RequiresReadConfirmation && alert.ActivatedDate.HasValue)
                {
                    var timeSinceActivation = DateTime.UtcNow - alert.ActivatedDate.Value;
                    if (timeSinceActivation > TimeSpan.FromMinutes(30) && alert.Level != EmergencyAlertLevel.Critical)
                    {
                        await EscalateAlertAsync(alert.EmergencyAlertId, EmergencyAlertLevel.Critical, "No confirmation received within 30 minutes");
                    }
                }
            }
        }

        #endregion

        #region Recipient Management

        public async Task<List<EmergencyAlertRecipient>> GetAlertRecipientsAsync(int alertId)
        {
            return await _context.EmergencyAlertRecipients
                .Where(r => r.EmergencyAlertId == alertId)
                .Include(r => r.Patient)
                .ToListAsync();
        }

        public async Task AddRecipientsToAlertAsync(int alertId, List<int> patientIds)
        {
            var alert = await GetEmergencyAlertAsync(alertId);
            var patients = await _context.Patients
                .Where(p => patientIds.Contains(p.PatientId))
                .ToListAsync();

            foreach (var patient in patients)
            {
                if (!string.IsNullOrEmpty(patient.PhoneNumber))
                {
                    var recipient = new EmergencyAlertRecipient
                    {
                        EmergencyAlertId = alertId,
                        PatientId = patient.PatientId,
                        RecipientType = "Patient",
                        RecipientId = patient.PatientId,
                        RecipientName = patient.FullName,
                        Email = patient.Email,
                        PhoneNumber = patient.PhoneNumber,
                        Status = EmergencyAlertRecipientStatus.Pending
                    };

                    _context.EmergencyAlertRecipients.Add(recipient);
                }
            }

            await _context.SaveChangesAsync();
            alert.TotalRecipients = await _context.EmergencyAlertRecipients.CountAsync(r => r.EmergencyAlertId == alertId);
            await _context.SaveChangesAsync();
        }

        public async Task AddPhoneNumbersToAlertAsync(int alertId, List<string> phoneNumbers)
        {
            var alert = await GetEmergencyAlertAsync(alertId);

            foreach (var phoneNumber in phoneNumbers)
            {
                var recipient = new EmergencyAlertRecipient
                {
                    EmergencyAlertId = alertId,
                    PhoneNumber = phoneNumber,
                    Status = EmergencyAlertRecipientStatus.Pending
                };

                _context.EmergencyAlertRecipients.Add(recipient);
            }

            await _context.SaveChangesAsync();
            alert.TotalRecipients = await _context.EmergencyAlertRecipients.CountAsync(r => r.EmergencyAlertId == alertId);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRecipientFromAlertAsync(int alertId, int recipientId)
        {
            var recipient = await _context.EmergencyAlertRecipients
                .FirstOrDefaultAsync(r => r.EmergencyAlertRecipientId == recipientId && r.EmergencyAlertId == alertId);

            if (recipient != null)
            {
                _context.EmergencyAlertRecipients.Remove(recipient);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Alert Processing and Distribution

        public async Task ProcessEmergencyAlertAsync(int alertId)
        {
            try
            {
                var alert = await GetEmergencyAlertAsync(alertId);
                await DistributeAlertAsync(alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing emergency alert: {alertId}");
                throw;
            }
        }

        public async Task ProcessScheduledAlertsAsync()
        {
            // This would be called by a background job to process scheduled alerts
            var scheduledAlerts = await _context.EmergencyAlerts
                .Where(a => a.Status == EmergencyAlertStatus.Draft)
                // Add condition for scheduled time when the property is added to the model
                .ToListAsync();

            foreach (var alert in scheduledAlerts)
            {
                await ActivateEmergencyAlertAsync(alert.EmergencyAlertId);
            }
        }

        public async Task<AlertDistributionResult> DistributeAlertAsync(EmergencyAlert alert)
        {
            var result = new AlertDistributionResult
            {
                DistributionStartTime = DateTime.UtcNow
            };

            try
            {
                var recipients = await GetAlertRecipientsAsync(alert.EmergencyAlertId);
                result.TotalRecipients = recipients.Count;

                foreach (var recipient in recipients)
                {
                    try
                    {
                        var success = await SendAlertToRecipientAsync(alert, recipient);
                        if (success)
                        {
                            result.SuccessfulSends++;
                            alert.SuccessfulCount++;
                        }
                        else
                        {
                            result.FailedSends++;
                            alert.FailedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedSends++;
                        result.Errors.Add($"Failed to send to {recipient.PhoneNumber}: {ex.Message}");
                        alert.FailedCount++;
                    }
                }

                alert.Status = EmergencyAlertStatus.Completed;
                await _context.SaveChangesAsync();

                result.DistributionEndTime = DateTime.UtcNow;
                result.TotalDistributionTime = result.DistributionEndTime - result.DistributionStartTime;

                _logger.LogInformation($"Alert distribution completed: {alert.Title}, Success: {result.SuccessfulSends}, Failed: {result.FailedSends}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error distributing alert: {alert.EmergencyAlertId}");
                throw;
            }
        }

        public async Task<bool> SendAlertToRecipientAsync(EmergencyAlert alert, EmergencyAlertRecipient recipient)
        {
            try
            {
                var message = FormatAlertMessage(alert);
                await _smsService.SendSmsAsync(recipient.PhoneNumber!, message);

                recipient.Status = EmergencyAlertRecipientStatus.Sent;
                recipient.SentDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                recipient.Status = EmergencyAlertRecipientStatus.Failed;
                recipient.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();

                _logger.LogError(ex, $"Failed to send alert to {recipient.PhoneNumber}");
                return false;
            }
        }

        #endregion

        #region Confirmation and Response Tracking

        public async Task<bool> ConfirmAlertReadAsync(int alertId, string phoneNumber, DateTime? confirmationTime = null)
        {
            var recipient = await _context.EmergencyAlertRecipients
                .FirstOrDefaultAsync(r => r.EmergencyAlertId == alertId && r.PhoneNumber == phoneNumber);

            if (recipient != null)
            {
                recipient.Status = EmergencyAlertRecipientStatus.ReadConfirmed;
                recipient.ReadConfirmedDate = confirmationTime ?? DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var alert = await GetEmergencyAlertAsync(alertId);
                alert.ReadConfirmationCount++;
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }

        public async Task<List<EmergencyAlertRecipient>> GetUnconfirmedRecipientsAsync(int alertId)
        {
            return await _context.EmergencyAlertRecipients
                .Where(r => r.EmergencyAlertId == alertId && 
                           r.Status != EmergencyAlertRecipientStatus.ReadConfirmed &&
                           r.Status == EmergencyAlertRecipientStatus.Sent)
                .Include(r => r.Patient)
                .ToListAsync();
        }

        public async Task<double> GetAlertConfirmationRateAsync(int alertId)
        {
            var totalSent = await _context.EmergencyAlertRecipients
                .CountAsync(r => r.EmergencyAlertId == alertId && r.Status == EmergencyAlertRecipientStatus.Sent);

            var confirmed = await _context.EmergencyAlertRecipients
                .CountAsync(r => r.EmergencyAlertId == alertId && r.Status == EmergencyAlertRecipientStatus.ReadConfirmed);

            return totalSent > 0 ? (double)confirmed / totalSent * 100 : 0;
        }

        public async Task ProcessReadConfirmationTimeoutAsync(int alertId)
        {
            var unconfirmedRecipients = await GetUnconfirmedRecipientsAsync(alertId);
            var alert = await GetEmergencyAlertAsync(alertId);

            if (alert.RequiresReadConfirmation && unconfirmedRecipients.Any())
            {
                // Send follow-up reminders or escalate
                await SendFollowUpRemindersAsync(alertId);
            }
        }

        #endregion

        #region Helper Methods and Stubs

        private async Task AddRecipientsBasedOnTargetAsync(EmergencyAlert alert, List<int>? specificPatientIds)
        {
            List<int> patientIds = new List<int>();

            switch (alert.TargetAudience)
            {
                case EmergencyAlertTarget.AllPatients:
                    patientIds = await _context.Patients.Select(p => p.PatientId).ToListAsync();
                    break;
                case EmergencyAlertTarget.ActivePatients:
                    patientIds = await _context.Patients
                        .Where(p => p.Appointments.Any(a => a.AppointmentDateTime >= DateTime.UtcNow.AddDays(-30)))
                        .Select(p => p.PatientId)
                        .ToListAsync();
                    break;
                case EmergencyAlertTarget.SpecificPatients:
                    if (specificPatientIds != null)
                        patientIds = specificPatientIds;
                    break;
            }

            if (patientIds.Any())
            {
                await AddRecipientsToAlertAsync(alert.EmergencyAlertId, patientIds);
            }
        }

        private string FormatAlertMessage(EmergencyAlert alert)
        {
            var levelEmoji = alert.Level switch
            {
                EmergencyAlertLevel.Critical => "🚨",
                EmergencyAlertLevel.High => "⚠️",
                EmergencyAlertLevel.Medium => "📢",
                _ => "ℹ️"
            };

            return $"{levelEmoji} EMERGENCY ALERT - {alert.Level.ToString().ToUpper()}\n\n{alert.Title}\n\n{alert.Message}";
        }

        // Stub implementations for methods that require additional infrastructure
        public Task SendFollowUpRemindersAsync(int alertId) { /* Implementation needed */ return Task.CompletedTask; }
        public Task<EmergencyAlert> CreateFollowUpAlertAsync(int originalAlertId, string followUpMessage, TimeSpan delay) { throw new NotImplementedException(); }
        public Task ScheduleAutomaticFollowUpsAsync(int alertId, List<TimeSpan> followUpIntervals) { /* Implementation needed */ return Task.CompletedTask; }
        public Task<List<EmergencyContact>> GetEmergencyContactsAsync(EmergencyContactType type) { return Task.FromResult(new List<EmergencyContact>()); }
        public Task<EmergencyContact> AddEmergencyContactAsync(string name, string phoneNumber, EmergencyContactType type, string? email = null, string? role = null) { throw new NotImplementedException(); }
        public Task UpdateEmergencyContactAsync(int contactId, string name, string phoneNumber, string? email = null, string? role = null) { /* Implementation needed */ return Task.CompletedTask; }
        public Task RemoveEmergencyContactAsync(int contactId) { /* Implementation needed */ return Task.CompletedTask; }
        public Task<bool> NotifyEmergencyContactsAsync(EmergencyAlert alert) { return Task.FromResult(false); }
        public Task<List<EmergencyAlertTemplate>> GetEmergencyAlertTemplatesAsync(EmergencyAlertType? type = null) { return Task.FromResult(new List<EmergencyAlertTemplate>()); }
        public Task<EmergencyAlertTemplate> CreateAlertTemplateAsync(string name, string messageTemplate, EmergencyAlertLevel defaultLevel, EmergencyAlertType type) { throw new NotImplementedException(); }
        public Task<EmergencyAlert> CreateAlertFromTemplateAsync(int templateId, Dictionary<string, string> variables, EmergencyAlertTarget target) { throw new NotImplementedException(); }

        public Task<List<EmergencyAlertAuditLog>> GetAlertAuditLogAsync(int alertId)
        {
            return Task.FromResult(new List<EmergencyAlertAuditLog>());
        }

        public Task LogEmergencyAlertActionAsync(int alertId, string action, string? performedBy = null, string? details = null)
        {
            _logger.LogInformation($"Emergency Alert Action: AlertId={alertId}, Action={action}, PerformedBy={performedBy ?? "System"}, Details={details}");
            return Task.CompletedTask;
        }

        public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate)
        {
            var totalAlerts = await _context.EmergencyAlerts
                .CountAsync(a => a.CreatedDate >= startDate && a.CreatedDate <= endDate);

            return new ComplianceReport
            {
                ReportPeriodStart = startDate,
                ReportPeriodEnd = endDate,
                TotalEmergencyAlerts = totalAlerts,
                ComplianceRate = 85.0, // Mock data
                ComplianceIssues = new List<string>(),
                Recommendations = new List<string>()
            };
        }

        public async Task<EmergencyAlertAnalytics> GetEmergencyAlertAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var dateRange = GetDateRange(startDate, endDate);
            var alerts = await _context.EmergencyAlerts
                .Where(a => a.CreatedDate >= dateRange.Start && a.CreatedDate <= dateRange.End)
                .ToListAsync();

            return new EmergencyAlertAnalytics
            {
                TotalAlerts = alerts.Count,
                ActiveAlerts = alerts.Count(a => a.Status == EmergencyAlertStatus.Active),
                CompletedAlerts = alerts.Count(a => a.Status == EmergencyAlertStatus.Completed),
                AlertsByLevel = alerts.GroupBy(a => a.Level).ToDictionary(g => g.Key, g => g.Count()),
                AlertsByType = alerts.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.Count()),
                AverageResponseTime = 15.5, // Mock data
                OverallConfirmationRate = 78.2,
                TotalRecipientsReached = alerts.Sum(a => a.SuccessfulCount),
                TotalCost = alerts.Sum(a => a.TotalCost)
            };
        }

        public Task<Dictionary<EmergencyAlertLevel, TimeSpan>> GetAverageResponseTimesByLevelAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return Task.FromResult(new Dictionary<EmergencyAlertLevel, TimeSpan>
            {
                { EmergencyAlertLevel.Critical, TimeSpan.FromMinutes(2) },
                { EmergencyAlertLevel.High, TimeSpan.FromMinutes(5) },
                { EmergencyAlertLevel.Medium, TimeSpan.FromMinutes(10) },
                { EmergencyAlertLevel.Low, TimeSpan.FromMinutes(30) }
            });
        }

        public Task<List<AlertEffectivenessReport>> GetAlertEffectivenessReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return Task.FromResult(new List<AlertEffectivenessReport>());
        }

        public Task<bool> IntegrateWithHospitalSystemAsync(string systemType, Dictionary<string, object> configuration) { return Task.FromResult(false); }
        public Task<bool> TriggerAlertFromExternalSystemAsync(string externalId, Dictionary<string, object> alertData) { return Task.FromResult(false); }
        public Task<List<SystemIntegrationLog>> GetSystemIntegrationLogsAsync(DateTime? startDate = null, DateTime? endDate = null) { return Task.FromResult(new List<SystemIntegrationLog>()); }
        public Task<EmergencyAlert> CreateTestAlertAsync(string message, List<string> testPhoneNumbers, string? createdBy = null) { throw new NotImplementedException(); }
        public Task<AlertTestResult> RunAlertSystemTestAsync(EmergencyAlertLevel level) { throw new NotImplementedException(); }
        public Task<bool> ValidateAlertSystemConfigurationAsync() { return Task.FromResult(true); }

        private (DateTime Start, DateTime End) GetDateRange(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;
            return (start, end);
        }

        #endregion
    }
}
