using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FixFlow.Infrastructure.Automation;

public class AutomationJobs
{
    private readonly AppDbContext _db;
    private readonly IAssignmentService _assignment;
    private readonly IRequestEventPublisher _events;
    private readonly AutomationSettings _auto;
    private readonly ILogger<AutomationJobs> _logger;

    public AutomationJobs(
        AppDbContext db,
        IAssignmentService assignment,
        IRequestEventPublisher events,
        IOptions<AutomationSettings> auto,
        ILogger<AutomationJobs> logger)
    {
        _db = db;
        _assignment = assignment;
        _events = events;
        _auto = auto.Value;
        _logger = logger;
    }

    // ---------- 1) مهلة رد الفني (Job مجدول وقت التعيين) ----------

    public async Task CheckOfferTimeoutAsync(int requestId, int technicianId)
    {
        var request = await _db.ServiceRequests
            .Include(r => r.TechnicianProfile)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        // لو الفني رد أو الطلب اتغير، مفيش حاجة نعملها
        if (request is null ||
            request.Status != RequestStatus.Assigned ||
            request.TechnicianProfileId != technicianId)
            return;

        await ExpireOfferAsync(request);
    }

    // شبكة أمان (كل دقيقة): لو الـ Job المجدول ضاع لأي سبب
    public async Task ExpireOverdueOffersAsync()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-30);

        var ids = await _db.ServiceRequests
            .Where(r => r.Status == RequestStatus.Assigned
                        && r.AssignmentExpiresAt != null
                        && r.AssignmentExpiresAt < cutoff)
            .Select(r => r.Id)
            .Take(50)
            .ToListAsync();

        foreach (var id in ids)
        {
            try
            {
                var request = await _db.ServiceRequests
                    .Include(r => r.TechnicianProfile)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request is { Status: RequestStatus.Assigned, TechnicianProfileId: not null })
                    await ExpireOfferAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire offer for request {RequestId}", id);
                _db.ChangeTracker.Clear();
            }
        }
    }

    private async Task ExpireOfferAsync(ServiceRequest request)
    {
        var techId = request.TechnicianProfileId!.Value;
        var techUserId = request.TechnicianProfile?.UserId;

        // نسجل إن الفني ده "رفض" بسبب انتهاء المهلة، عشان الخوارزمية ما ترجعلوش الطلب
        _db.RequestRejections.Add(new RequestRejection
        {
            ServiceRequestId = request.Id,
            TechnicianProfileId = techId,
            Reason = "Timeout: no response"
        });

        request.Status = RequestStatus.Pending;
        request.TechnicianProfileId = null;
        request.TechnicianProfile = null;
        request.AssignedAt = null;
        request.AssignmentExpiresAt = null;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _events.PublishAsync(request.Id, RequestEventType.OfferExpired, techUserId);
        await _assignment.TryAssignAsync(request.Id);
    }

    // ---------- 2) إعادة محاولة التعيين للطلبات المعلقة (كل دقيقة) ----------

    public async Task RetryPendingAssignmentsAsync()
    {
        // نتجاهل الطلبات اللي لسه متحركة من ثواني، عشان ما نتصادمش مع التعيين الفوري
        var cutoff = DateTime.UtcNow.AddSeconds(-15);

        var ids = await _db.ServiceRequests
            .Where(r => r.Status == RequestStatus.Pending && (r.UpdatedAt ?? r.CreatedAt) < cutoff)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.Id)
            .Take(50)
            .ToListAsync();

        foreach (var id in ids)
        {
            try
            {
                await _assignment.TryAssignAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Retry assignment failed for request {RequestId}", id);
                _db.ChangeTracker.Clear();
            }
        }
    }

    // ---------- 3) تصعيد الطلبات الواقفة (كل 15 دقيقة) ----------

    public async Task EscalateStaleRequestsAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-_auto.StaleAfterMinutes);

        var ids = await _db.ServiceRequests
            .Where(r => r.EscalatedAt == null
                        && r.Status != RequestStatus.Completed
                        && r.Status != RequestStatus.Reviewed
                        && r.Status != RequestStatus.Cancelled
                        && r.Status != RequestStatus.Rejected
                        && (r.UpdatedAt ?? r.CreatedAt) < cutoff)
            .Select(r => r.Id)
            .Take(50)
            .ToListAsync();

        foreach (var id in ids)
        {
            try
            {
                var request = await _db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id);
                if (request is null || request.EscalatedAt is not null)
                    continue;

                request.EscalatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                await _events.PublishAsync(id, RequestEventType.Escalated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Escalation failed for request {RequestId}", id);
                _db.ChangeTracker.Clear();
            }
        }
    }
}