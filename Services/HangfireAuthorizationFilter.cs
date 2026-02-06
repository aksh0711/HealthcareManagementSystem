using Hangfire.Dashboard;

namespace HealthcareManagementSystem.Services
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // In development, allow all access
            // In production, you should implement proper authorization
            return true;
        }
    }
}
