using DnTech_Ecommerce.Data;
using DnTech_Ecommerce.Models;
using DnTech_Ecommerce.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DnTech_Ecommerce.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Crear y enviar una notificación a un usuario específico
        /// </summary>
        public async Task<Notification> CreateAndSendNotification(
            string userId,
            string message,
            NotificationType type,
            string? link = null,
            int? orderId = null)
        {
            try
            {
                // Crear la notificación en la base de datos
                var notification = new Notification
                {
                    UserId = userId,
                    Message = message,
                    Type = type,
                    Link = link,
                    OrderId = orderId,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Enviar notificación en tiempo real vía SignalR
                await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", new
                {
                    id = notification.Id,
                    message = notification.Message,
                    type = notification.Type.ToString(),
                    link = notification.Link,
                    createdAt = notification.CreatedAt,
                    isRead = notification.IsRead
                });

                _logger.LogInformation($"Notification sent to user {userId}: {message}");

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating notification: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Enviar notificación a todos los administradores
        /// </summary>
        public async Task NotifyAdmins(string message, NotificationType type, string? link = null, int? orderId = null)
        {
            try
            {
                // Obtener todos los usuarios con rol Administrator
                var adminUsers = await _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                    .Where(x => x.r.Name == "Administrator")
                    .Select(x => x.ur.UserId)
                    .ToListAsync();

                foreach (var adminId in adminUsers)
                {
                    await CreateAndSendNotification(adminId, message, type, link, orderId);
                }

                _logger.LogInformation($"Notification sent to {adminUsers.Count} administrators");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying admins: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Marcar una notificación como leída
        /// </summary>
        public async Task<bool> MarkAsRead(int notificationId, string userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                    return false;

                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking notification as read: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Marcar todas las notificaciones de un usuario como leídas
        /// </summary>
        public async Task<int> MarkAllAsRead(string userId)
        {
            try
            {
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return unreadNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking all notifications as read: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Obtener notificaciones de un usuario
        /// </summary>
        public async Task<List<Notification>> GetUserNotifications(string userId, int count = 10, bool unreadOnly = false)
        {
            try
            {
                var query = _context.Notifications
                    .Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting user notifications: {ex.Message}");
                return new List<Notification>();
            }
        }

        /// <summary>
        /// Contar notificaciones no leídas
        /// </summary>
        public async Task<int> GetUnreadCount(string userId)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting unread count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Eliminar notificaciones antiguas (opcional - para limpieza)
        /// </summary>
        public async Task<int> DeleteOldNotifications(int daysOld = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysOld);

                var oldNotifications = await _context.Notifications
                    .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                    .ToListAsync();

                _context.Notifications.RemoveRange(oldNotifications);
                await _context.SaveChangesAsync();

                return oldNotifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting old notifications: {ex.Message}");
                return 0;
            }
        }
    }
}