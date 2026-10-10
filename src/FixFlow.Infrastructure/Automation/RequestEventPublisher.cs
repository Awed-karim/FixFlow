using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Notifications;
using FixFlow.Application.Requests;
using FixFlow.Domain.Entities;
using FixFlow.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FixFlow.Infrastructure.Automation;

public class RequestEventPublisher : IRequestEventPublisher
{
    private readonly AppDbContext _db;
    private readonly IRealtimeNotifier _realtime;
    private readonly IBackgroundJobClient _jobs;
    private readonly AutomationSettings _auto;
    private readonly AppUrlSettings _urls;
    private readonly WebhookSettings _webhook;
    private readonly ILogger<RequestEventPublisher> _logger;

    public RequestEventPublisher(
        AppDbContext db,
        IRealtimeNotifier realtime,
        IBackgroundJobClient jobs,
        IOptions<AutomationSettings> auto,
        IOptions<AppUrlSettings> urls,
        IOptions<WebhookSettings> webhook,
        ILogger<RequestEventPublisher> logger)
    {
        _db = db;
        _realtime = realtime;
        _jobs = jobs;
        _auto = auto.Value;
        _urls = urls.Value;
        _webhook = webhook.Value;
        _logger = logger;
    }

    public async Task PublishAsync(int requestId, RequestEventType type, string? relatedUserId = null)
    {
        try
        {
            await PublishCoreAsync(requestId, type, relatedUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {Event} for request {RequestId}", type, requestId);
        }
    }

    private async Task PublishCoreAsync(int requestId, RequestEventType type, string? relatedUserId)
    {
        var r = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.TechnicianProfile)
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (r is null)
            return;

        var tech = r.TechnicianProfile;
        var links = QuickShareLinks.Build(_urls.TrackingBaseUrl, r.TrackingToken, r.Title);

        // ---------- 1) تحديث لحظي (SignalR) ----------
        var update = new RequestUpdateMessage
        {
            RequestId = r.Id,
            Title = r.Title,
            Status = r.Status.ToString(),
            Event = type.ToString(),
            TechnicianName = tech?.FullName,
            UpdatedAt = DateTime.UtcNow
        };

        await _realtime.SendToUserAsync(r.CustomerId, "RequestUpdated", update);
        if (tech is not null)
            await _realtime.SendToUserAsync(tech.UserId, "RequestUpdated", update);
        await _realtime.SendToAdminsAsync("RequestUpdated", update);
        await _realtime.SendToTrackingAsync(r.TrackingToken, "RequestUpdated", update);

        // ---------- 2) إشعارات محفوظة في الداتابيز ----------
        var targets = await BuildTargetsAsync(r, type, links, relatedUserId);

        if (targets.Count > 0)
        {
            var entities = targets.Select(t => new Notification
            {
                UserId = t.UserId,
                Message = t.Message.Length > 500 ? t.Message[..500] : t.Message,
                Type = type.ToString(),
                ServiceRequestId = r.Id
            }).ToList();

            _db.Notifications.AddRange(entities);
            await _db.SaveChangesAsync();

            foreach (var n in entities)
            {
                await _realtime.SendToUserAsync(n.UserId, "NotificationReceived", new NotificationDto
                {
                    Id = n.Id,
                    Message = n.Message,
                    Type = n.Type,
                    ServiceRequestId = n.ServiceRequestId,
                    IsRead = false,
                    CreatedAt = n.CreatedAt
                });
            }
        }

        // ---------- 3) Webhook (Power Automate) عن طريق Hangfire ----------
        if (_webhook.Enabled && !string.IsNullOrWhiteSpace(_webhook.Url))
        {
            var eventName = type.ToString();
            _jobs.Enqueue<WebhookJob>(j => j.SendRequestEventAsync(requestId, eventName));
        }
    }

    // مين يستلم إيه لكل حدث
    private async Task<List<(string UserId, string Message)>> BuildTargetsAsync(
        ServiceRequest r, RequestEventType type, ShareLinks links, string? relatedUserId)
    {
        var list = new List<(string UserId, string Message)>();
        var tech = r.TechnicianProfile;
        var techName = tech?.FullName ?? "الفني";

        switch (type)
        {
            case RequestEventType.Created:
                list.Add((r.CustomerId, $"تم استلام طلبك «{r.Title}» وجاري البحث عن فني مناسب."));
                break;

            case RequestEventType.Assigned:
                list.Add((r.CustomerId, $"تم ترشيح الفني {techName} لطلبك «{r.Title}» وفي انتظار قبوله."));
                if (tech is not null)
                    list.Add((tech.UserId,
                        $"طلب جديد «{r.Title}» في {r.Address}. عندك {_auto.OfferTimeoutSeconds} ثانية للقبول أو الرفض."));
                break;

            case RequestEventType.Accepted:
                list.Add((r.CustomerId,
                    $"الفني {techName} قبل طلبك «{r.Title}». للتواصل: {tech?.PhoneNumber}. تابع الطلب من هنا: {links.TrackingUrl}"));
                break;

            case RequestEventType.OfferRejected:
                list.Add((r.CustomerId, "الفني اعتذر عن الطلب، وجاري البحث عن فني آخر."));
                break;

            case RequestEventType.OfferExpired:
                list.Add((r.CustomerId, "الفني لم يرد في الوقت المحدد، وجاري البحث عن فني آخر."));
                if (!string.IsNullOrEmpty(relatedUserId))
                    list.Add((relatedUserId, $"انتهت مهلة الرد على طلب «{r.Title}» وتم تحويله لفني آخر."));
                break;

            case RequestEventType.OnTheWay:
                list.Add((r.CustomerId, $"الفني {techName} في الطريق إليك."));
                break;

            case RequestEventType.WorkStarted:
                list.Add((r.CustomerId, $"بدأ الفني {techName} العمل على طلبك."));
                break;

            case RequestEventType.Completed:
                list.Add((r.CustomerId,
                    $"تم الانتهاء من الإصلاح. راجع صور قبل/بعد وقيّم الخدمة. التفاصيل: {links.TrackingUrl}"));
                break;

            case RequestEventType.Reviewed:
                if (tech is not null)
                    list.Add((tech.UserId,
                        $"العميل قيّم خدمتك في طلب «{r.Title}». تقييمك الحالي: {tech.AverageRating}"));
                break;

            case RequestEventType.Cancelled:
                list.Add((r.CustomerId, $"تم إلغاء الطلب «{r.Title}»."));
                if (tech is not null)
                    list.Add((tech.UserId, $"تم إلغاء الطلب «{r.Title}»."));
                break;

            case RequestEventType.Escalated:
                var adminIds = await GetAdminIdsAsync();
                foreach (var adminId in adminIds)
                    list.Add((adminId,
                        $"تنبيه: الطلب #{r.Id} «{r.Title}» متوقف منذ أكثر من {_auto.StaleAfterMinutes} دقيقة (الحالة: {r.Status})."));
                break;
        }

        return list;
    }

    private async Task<List<string>> GetAdminIdsAsync()
    {
        var adminRoleId = await _db.Roles
            .Where(x => x.Name == AppRoles.Admin)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();

        if (adminRoleId is null)
            return new List<string>();

        return await _db.UserRoles
            .Where(ur => ur.RoleId == adminRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync();
    }
}