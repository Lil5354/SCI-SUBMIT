using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Services;
using System.Security.Claims;

namespace SciSubmit.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationBellViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdClaim = ViewContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return View(new NotificationBellViewModel
                {
                    UnreadCount = 0,
                    RecentNotifications = new List<NotificationBellItemViewModel>()
                });
            }

            var unreadCount = await _notificationService.GetUnreadCountAsync(userId);
            var recentNotifications = await _notificationService.GetRecentNotificationsAsync(userId, 10);

            var viewModel = new NotificationBellViewModel
            {
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications.Select(n => new NotificationBellItemViewModel
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    Status = n.Status,
                    CreatedAt = n.CreatedAt,
                    RelatedSubmissionId = n.RelatedSubmissionId,
                    SubmissionTitle = n.RelatedSubmission?.Title
                }).ToList()
            };

            return View(viewModel);
        }
    }

    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationBellItemViewModel> RecentNotifications { get; set; } = new();
    }

    public class NotificationBellItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Models.Enums.NotificationType Type { get; set; }
        public Models.Enums.NotificationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedSubmissionId { get; set; }
        public string? SubmissionTitle { get; set; }
    }
}

