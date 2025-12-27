using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SciSubmit.Services;
using System.Security.Claims;

namespace SciSubmit.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return 0;
        }

        /// <summary>
        /// API: Lấy số lượng unread notifications
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { count = 0 });
                }

                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return Json(new { count = 0 });
            }
        }

        /// <summary>
        /// API: Lấy danh sách notifications
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications(int page = 1, int pageSize = 20, bool? isRead = null)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { notifications = new List<object>(), totalCount = 0 });
                }

                var (notifications, totalCount) = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize, isRead);

                var result = notifications.Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type.ToString(),
                    status = n.Status.ToString(),
                    createdAt = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    relatedSubmissionId = n.RelatedSubmissionId,
                    submissionTitle = n.RelatedSubmission?.Title
                }).ToList();

                return Json(new { notifications = result, totalCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return Json(new { notifications = new List<object>(), totalCount = 0 });
            }
        }

        /// <summary>
        /// API: Đánh dấu notification là đã đọc
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _notificationService.MarkAsReadAsync(id, userId);
                if (success)
                {
                    var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
                    return Json(new { success = true, unreadCount });
                }

                return Json(new { success = false, message = "Notification not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// API: Đánh dấu tất cả notifications là đã đọc
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var count = await _notificationService.MarkAllAsReadAsync(userId);
                return Json(new { success = true, count, unreadCount = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// API: Xóa notification
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var success = await _notificationService.DeleteNotificationAsync(id, userId);
                if (success)
                {
                    var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
                    return Json(new { success = true, unreadCount });
                }

                return Json(new { success = false, message = "Notification not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return Json(new { success = false, message = "An error occurred" });
            }
        }

        /// <summary>
        /// Trang danh sách notifications đầy đủ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string filter = "all", int page = 1)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return RedirectToAction("Login", "Account");
                }

                bool? isRead = filter switch
                {
                    "unread" => false,
                    "read" => true,
                    _ => null
                };

                var pageSize = 20;
                var (notifications, totalCount) = await _notificationService.GetUserNotificationsAsync(userId, page, pageSize, isRead);
                var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

                ViewBag.Filter = filter;
                ViewBag.Page = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.TotalCount = totalCount;
                ViewBag.UnreadCount = unreadCount;

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications page");
                TempData["ErrorMessage"] = "An error occurred while loading notifications.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}

