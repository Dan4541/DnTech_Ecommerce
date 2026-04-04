using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DnTech_Ecommerce.Services;
using System.Security.Claims;

namespace DnTech_Ecommerce.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly NotificationService _notificationService;

        public NotificationsController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: /Notifications
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var notifications = await _notificationService.GetUserNotifications(userId, 50);
            return View(notifications);
        }

        // GET: /Notifications/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _notificationService.GetUnreadCount(userId);
            return Json(new { count });
        }

        // GET: /Notifications/GetRecent
        [HttpGet]
        public async Task<IActionResult> GetRecent(int count = 5)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new List<object>());
            }

            var notifications = await _notificationService.GetUserNotifications(userId, count);
            
            var result = notifications.Select(n => new
            {
                id = n.Id,
                message = n.Message,
                type = n.Type.ToString(),
                link = n.Link,
                isRead = n.IsRead,
                createdAt = n.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });

            return Json(result);
        }

        // POST: /Notifications/MarkAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false });
            }

            var success = await _notificationService.MarkAsRead(id, userId);
            return Json(new { success });
        }

        // POST: /Notifications/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, count = 0 });
            }

            var count = await _notificationService.MarkAllAsRead(userId);
            return Json(new { success = true, count });
        }
    }
}