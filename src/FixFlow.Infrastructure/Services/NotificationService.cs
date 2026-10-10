using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Notifications;
using FixFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<NotificationDto>> GetMineAsync(
        string userId, bool unreadOnly, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                ServiceRequestId = n.ServiceRequestId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<NotificationDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public Task<int> GetUnreadCountAsync(string userId)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<Result> MarkAsReadAsync(int id, string userId)
    {
        // بنفلتر بالـ userId: إشعار مستخدم تاني بيرجع "مش موجود" مش "ممنوع"
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (notification is null)
            return Result.Fail("Notification not found.", ErrorType.NotFound);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Result.Ok();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.UpdatedAt, (DateTime?)DateTime.UtcNow));
    }
}