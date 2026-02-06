using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace HealthcareManagementSystem.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendNotificationToUser(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        public async Task SendNotificationToRole(string role, string message)
        {
            await Clients.Group(role).SendAsync("ReceiveNotification", message);
        }

        public async Task NotifyAppointmentUpdate(string appointmentId, string status, string message)
        {
            await Clients.All.SendAsync("AppointmentUpdated", appointmentId, status, message);
        }

        public async Task NotifyEmergency(string message, string location)
        {
            await Clients.Group("Doctors").SendAsync("EmergencyAlert", message, location);
            await Clients.Group("Nurses").SendAsync("EmergencyAlert", message, location);
            await Clients.Group("Admin").SendAsync("EmergencyAlert", message, location);
        }

        public override async Task OnConnectedAsync()
        {
            var userRoles = Context.User?.Claims
                .Where(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value);

            if (userRoles != null)
            {
                foreach (var role in userRoles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, role);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userRoles = Context.User?.Claims
                .Where(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value);

            if (userRoles != null)
            {
                foreach (var role in userRoles)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, role);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
