using FixFlow.Application.Common;
using FixFlow.Application.Notifications;

namespace FixFlow.Application.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetMineAsync(string userId, bool unreadOnly, int page, int pageSize);
    Task<int> GetUnreadCountAsync(string userId);
    Task<Result> MarkAsReadAsync(int id, string userId);
    Task MarkAllAsReadAsync(string userId);
}