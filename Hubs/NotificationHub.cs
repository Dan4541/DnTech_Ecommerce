using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace DnTech_Ecommerce.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            
            if (!string.IsNullOrEmpty(userId))
            {
                // Agregar el usuario a un grupo con su ID
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
                
                Console.WriteLine($"User {userId} connected to NotificationHub");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                
                Console.WriteLine($"User {userId} disconnected from NotificationHub");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Método para que el cliente marque notificaciones como leídas
        public async Task MarkAsRead(int notificationId)
        {
            // Este método será llamado desde el cliente
            // El procesamiento se hará en el NotificationService
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }
    }
}