using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Services;

public class AssignmentService : IAssignmentService
{
    private const double MaxDistanceKm = 50;       // أبعد مسافة مقبولة
    private const double RatingWeightKm = 2;       // كل نجمة تقييم = ميزة 2 كم
    private const int OfferTimeoutMinutes = 2;     // مهلة رد الفني

    private readonly AppDbContext _db;

    public AssignmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryAssignAsync(int requestId)
    {
        var request = await _db.ServiceRequests
            .Include(r => r.Rejections)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request is null || request.Status != RequestStatus.Pending)
            return false;

        // 1) فنيين رفضوا الطلب ده قبل كده
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
        request.AssignmentExpiresAt = now.AddMinutes(OfferTimeoutMinutes);
        request.UpdatedAt = now;

        await _db.SaveChangesAsync();
        return true;
    }
}