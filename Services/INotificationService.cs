using SciSubmit.Models.Notification;

namespace SciSubmit.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Tạo notification mới cho user
        /// </summary>
        Task<UserNotification> CreateNotificationAsync(UserNotification notification);

        /// <summary>
        /// Lấy danh sách notifications của user với pagination
        /// </summary>
        Task<(List<UserNotification> Notifications, int TotalCount)> GetUserNotificationsAsync(
            int userId, 
            int page = 1, 
            int pageSize = 20,
            bool? isRead = null);

        /// <summary>
        /// Lấy số lượng unread notifications của user
        /// </summary>
        Task<int> GetUnreadCountAsync(int userId);

        /// <summary>
        /// Đánh dấu notification là đã đọc
        /// </summary>
        Task<bool> MarkAsReadAsync(int notificationId, int userId);

        /// <summary>
        /// Đánh dấu tất cả notifications của user là đã đọc
        /// </summary>
        Task<int> MarkAllAsReadAsync(int userId);

        /// <summary>
        /// Xóa notification (chỉ user sở hữu mới xóa được)
        /// </summary>
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);

        /// <summary>
        /// Lấy các notifications mới nhất (cho dropdown)
        /// </summary>
        Task<List<UserNotification>> GetRecentNotificationsAsync(int userId, int count = 10);
    }
}

