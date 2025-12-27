using Microsoft.EntityFrameworkCore;
using SciSubmit.Data;
using SciSubmit.Models.Notification;
using SciSubmit.Models.Enums;

namespace SciSubmit.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserNotification> CreateNotificationAsync(UserNotification notification)
        {
            try
            {
                notification.CreatedAt = DateTime.UtcNow;
                notification.Status = NotificationStatus.Unread;
                _context.UserNotifications.Add(notification);
                await _context.SaveChangesAsync();
                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification for user {UserId}", notification.UserId);
                throw;
            }
        }

        public async Task<(List<UserNotification> Notifications, int TotalCount)> GetUserNotificationsAsync(
            int userId, 
            int page = 1, 
            int pageSize = 20,
            bool? isRead = null)
        {
            try
            {
                var query = _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .Include(n => n.RelatedSubmission)
                    .Include(n => n.RelatedUser)
                    .OrderByDescending(n => n.CreatedAt)
                    .AsQueryable();

                // Filter by read status if provided
                if (isRead.HasValue)
                {
                    query = isRead.Value
                        ? query.Where(n => n.Status == NotificationStatus.Read)
                        : query.Where(n => n.Status == NotificationStatus.Unread);
                }

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return (notifications, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            try
            {
                return await _context.UserNotifications
                    .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return false;
                }

                if (notification.Status == NotificationStatus.Unread)
                {
                    notification.Status = NotificationStatus.Read;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
                return false;
            }
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var unreadNotifications = await _context.UserNotifications
                    .Where(n => n.UserId == userId && n.Status == NotificationStatus.Unread)
                    .ToListAsync();

                var count = unreadNotifications.Count;
                var now = DateTime.UtcNow;

                foreach (var notification in unreadNotifications)
                {
                    notification.Status = NotificationStatus.Read;
                    notification.ReadAt = now;
                }

                await _context.SaveChangesAsync();
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
                return 0;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

                if (notification == null)
                {
                    return false;
                }

                _context.UserNotifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId} for user {UserId}", notificationId, userId);
                return false;
            }
        }

        public async Task<List<UserNotification>> GetRecentNotificationsAsync(int userId, int count = 10)
        {
            try
            {
                return await _context.UserNotifications
                    .Where(n => n.UserId == userId)
                    .Include(n => n.RelatedSubmission)
                    .Include(n => n.RelatedUser)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications for user {UserId}", userId);
                return new List<UserNotification>();
            }
        }
    }
}

