using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Automation;
using FixFlow.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FixFlow.Infrastructure.Services;

public class AssignmentService : IAssignmentService
{
    private const double MaxDistanceKm = 50;       // أبعد مسافة مقبولة
    private const double RatingWeightKm = 2;       // كل نجمة تقييم = ميزة 2 كم

    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;
    private readonly IRequestEventPublisher _events;
    private readonly AutomationSettings _auto;

    public AssignmentService(
        AppDbContext db,
        IBackgroundJobClient jobs,
        IRequestEventPublisher events,
        IOptions<AutomationSettings> auto)
    {
        _db = db;
        _jobs = jobs;
        _events = events;
        _auto = auto.Value;
    }

    public async Task<bool> TryAssignAsync(int requestId)
    {
        var request = await _db.ServiceRequests
            .Include(r => r.Rejections)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null || request.Status != RequestStatus.Pending)
            return false;

        // 1) فنيين رفضوا الطلب ده (أو انتهت مهلتهم)
        var rejectedIds = request.Rejections.Select(x => x.TechnicianProfileId).ToList();

        // 2) فنيين مشغولين (عندهم طلب شغال)
        var busyIds = await _db.ServiceRequests
            .Where(r => r.TechnicianProfileId != null &&
                        (r.Status == RequestStatus.Assigned ||
                         r.Status == RequestStatus.Accepted ||
                         r.Status == RequestStatus.OnTheWay ||
                         r.Status == RequestStatus.InProgress))
            .Select(r => r.TechnicianProfileId!.Value)
            .Distinct()
            .ToListAsync();

        // 3) المرشحين: نفس التخصص + متاح + مش مرفوض + مش مشغول
        var candidates = await _db.TechnicianProfiles
            .Where(t => t.ServiceCategoryId == request.ServiceCategoryId
                        && t.IsAvailable
                        && !rejectedIds.Contains(t.Id)
                        && !busyIds.Contains(t.Id))
            .ToListAsync();

        // 4) المسافة + التقييم: الأقل في (المسافة - التقييم × 2) هو الأفضل
        var best = candidates
            .Select(t => new
            {
                Tech = t,
                Km = GeoMath.DistanceKm(request.Latitude, request.Longitude, t.Latitude, t.Longitude)
            })
            .Where(x => x.Km <= MaxDistanceKm)
            .OrderBy(x => x.Km - x.Tech.AverageRating * RatingWeightKm)
            .ThenBy(x => x.Tech.Id)
            .FirstOrDefault();

        if (best is null)
            return false;

        var now = DateTime.UtcNow;
        request.TechnicianProfileId = best.Tech.Id;
        request.TechnicianProfile = best.Tech;
        request.Status = RequestStatus.Assigned;
        request.AssignedAt = now;
        request.AssignmentExpiresAt = now.AddSeconds(_auto.OfferTimeoutSeconds);
        request.UpdatedAt = now;

        await _db.SaveChangesAsync();

        // جدولة Job المهلة: لو الفني ما ردش، يتحول لفني تاني
        var requestIdLocal = request.Id;
        var techIdLocal = best.Tech.Id;
        try
        {
            _jobs.Schedule<AutomationJobs>(
                j => j.CheckOfferTimeoutAsync(requestIdLocal, techIdLocal),
                TimeSpan.FromSeconds(_auto.OfferTimeoutSeconds + 2));
        }
        catch
        {
            // لو الجدولة فشلت، شبكة الأمان (ExpireOverdueOffersAsync) هتلتقطه
        }

        await _events.PublishAsync(requestIdLocal, RequestEventType.Assigned);
        return true;
    }
}