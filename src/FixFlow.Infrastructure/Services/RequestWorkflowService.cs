using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Requests;
using FixFlow.Domain.Common;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Services;

public class RequestWorkflowService : IRequestWorkflowService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;   // 5 MB
    private const int MaxPhotosPerType = 10;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly AppDbContext _db;
    private readonly IAssignmentService _assignment;
    private readonly IFileStorage _storage;

    public RequestWorkflowService(AppDbContext db, IAssignmentService assignment, IFileStorage storage)
    {
        _db = db;
        _assignment = assignment;
        _storage = storage;
    }

    // ---------- الأدمن: إعادة محاولة التعيين ----------

    public async Task<Result<ServiceRequestDto>> AssignAsync(int id)
    {
        var request = await _db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
            return Result<ServiceRequestDto>.Fail("Request not found.", ErrorType.NotFound);

        if (request.Status != RequestStatus.Pending)
            return Result<ServiceRequestDto>.Fail("Only pending requests can be assigned.");

        var assigned = await _assignment.TryAssignAsync(id);
        if (!assigned)
            return Result<ServiceRequestDto>.Fail("No suitable technician is available right now.", ErrorType.Conflict);

        return Result<ServiceRequestDto>.Ok(await GetDtoAsync(id));
    }

    // ---------- الفني ----------

    public Task<Result<ServiceRequestDto>> AcceptAsync(int id, string userId)
        => TechnicianTransitionAsync(id, userId, RequestStatus.Accepted,
            apply: r => r.AssignmentExpiresAt = null);

    public Task<Result<ServiceRequestDto>> StartTravelAsync(int id, string userId)
        => TechnicianTransitionAsync(id, userId, RequestStatus.OnTheWay);

    public Task<Result<ServiceRequestDto>> StartWorkAsync(int id, string userId)
        => TechnicianTransitionAsync(id, userId, RequestStatus.InProgress);

    public Task<Result<ServiceRequestDto>> CompleteAsync(int id, string userId)
        => TechnicianTransitionAsync(id, userId, RequestStatus.Completed,
            validate: r =>
            {
                var hasBefore = r.Photos.Any(p => p.Type == PhotoType.Before);
                var hasAfter = r.Photos.Any(p => p.Type == PhotoType.After);
                return hasBefore && hasAfter
                    ? null
                    : "You must upload at least one Before photo and one After photo before completing.";
            },
            apply: r =>
            {
                r.CompletedAt = DateTime.UtcNow;
                if (r.TechnicianProfile is not null)
                    r.TechnicianProfile.CompletedJobsCount++;
            });

    public async Task<Result<ServiceRequestDto>> RejectAsync(int id, string userId, string? reason)
    {
        var load = await LoadForTechnicianAsync(id, userId);
        if (!load.Succeeded)
            return Result<ServiceRequestDto>.Fail(load.Error!, load.ErrorType);

        var request = load.Data!;

        // الرفض مسموح بس والطلب Assigned (Assigned → Pending)
        if (!RequestStateMachine.CanTransition(request.Status, RequestStatus.Pending))
            return Result<ServiceRequestDto>.Fail($"A request with status '{request.Status}' cannot be rejected.");

        _db.RequestRejections.Add(new RequestRejection
        {
            ServiceRequestId = request.Id,
            TechnicianProfileId = request.TechnicianProfileId!.Value,
            Reason = reason?.Trim()
        });

        request.Status = RequestStatus.Pending;
        request.TechnicianProfileId = null;
        request.TechnicianProfile = null;
        request.AssignedAt = null;
        request.AssignmentExpiresAt = null;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // التعيين التلقائي للفني اللي بعده
        await _assignment.TryAssignAsync(request.Id);

        return Result<ServiceRequestDto>.Ok(await GetDtoAsync(request.Id));
    }

    // ---------- العميل: تأكيد وتقييم ----------

    public async Task<Result<ServiceRequestDto>> ReviewAsync(int id, string userId, ReviewRequest review)
    {
        var request = await _db.ServiceRequests.WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
            return Result<ServiceRequestDto>.Fail("Request not found.", ErrorType.NotFound);

        if (request.CustomerId != userId)
            return Result<ServiceRequestDto>.Fail("You are not allowed to review this request.", ErrorType.Forbidden);

        if (!RequestStateMachine.CanTransition(request.Status, RequestStatus.Reviewed))
            return Result<ServiceRequestDto>.Fail("You can review a request only once, after it is completed.");

        var technician = request.TechnicianProfile;
        if (technician is null)
            return Result<ServiceRequestDto>.Fail("This request has no technician to review.");

        // متوسط التقييم الجديد = (مجموع القديم + الجديد) / (العدد القديم + 1)
        var previous = _db.Reviews.Where(r => r.ServiceRequest.TechnicianProfileId == technician.Id);
        var count = await previous.CountAsync();
        var sum = count == 0 ? 0 : await previous.SumAsync(r => r.Rating);

        technician.AverageRating = Math.Round((sum + review.Rating) / (double)(count + 1), 2);

        _db.Reviews.Add(new Review
        {
            ServiceRequestId = request.Id,
            Rating = review.Rating,
            Comment = review.Comment?.Trim()
        });

        request.Status = RequestStatus.Reviewed;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Result<ServiceRequestDto>.Ok(request.ToDto());
    }

    // ---------- الصور ----------

    public async Task<Result<PhotoDto>> UploadPhotoAsync(
        int id, string userId, string role, string type, FileUpload file)
    {
        if (!Enum.TryParse<PhotoType>(type, true, out var photoType) || !Enum.IsDefined(photoType))
            return Result<PhotoDto>.Fail("Invalid type. Allowed: Problem, Before, After.");

        if (file.Length <= 0)
            return Result<PhotoDto>.Fail("File is empty.");

        if (file.Length > MaxFileSizeBytes)
            return Result<PhotoDto>.Fail("File is too large. Maximum size is 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Result<PhotoDto>.Fail("Only JPG, PNG or WEBP images are allowed.");

        var request = await _db.ServiceRequests
            .Include(r => r.Photos)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
            return Result<PhotoDto>.Fail("Request not found.", ErrorType.NotFound);

        if (photoType == PhotoType.Problem)
        {
            // صورة المشكلة: العميل صاحب الطلب، قبل بداية الشغل
            if (role != AppRoles.Customer || request.CustomerId != userId)
                return Result<PhotoDto>.Fail("Only the customer who created the request can upload problem photos.", ErrorType.Forbidden);

            var open = request.Status is RequestStatus.Pending or RequestStatus.Assigned or RequestStatus.Accepted;
            if (!open)
                return Result<PhotoDto>.Fail("Problem photos can only be added before the technician starts.");
        }
        else
        {
            // قبل/بعد: الفني المعيّن، والطلب InProgress
            if (role != AppRoles.Technician)
                return Result<PhotoDto>.Fail("Only the assigned technician can upload Before/After photos.", ErrorType.Forbidden);

            var techId = await GetTechnicianProfileIdAsync(userId);
            if (techId is null || request.TechnicianProfileId != techId)
                return Result<PhotoDto>.Fail("This request is not assigned to you.", ErrorType.Forbidden);

            if (request.Status != RequestStatus.InProgress)
                return Result<PhotoDto>.Fail("Before/After photos can only be uploaded while the job is in progress.");
        }

        if (request.Photos.Count(p => p.Type == photoType) >= MaxPhotosPerType)
            return Result<PhotoDto>.Fail($"Maximum {MaxPhotosPerType} photos of type {photoType} per request.");

        var url = await _storage.SaveAsync(file.Content, extension, $"requests/{id}");

        _db.RequestPhotos.Add(new RequestPhoto
        {
            ServiceRequestId = id,
            Url = url,
            Type = photoType
        });
        await _db.SaveChangesAsync();

        return Result<PhotoDto>.Ok(new PhotoDto { Url = url, Type = photoType.ToString() });
    }

    // ---------- Helpers ----------

    // نمط واحد لكل انتقالات الفني: تحميل + صلاحية + State Machine + تحقق + تنفيذ + حفظ
    private async Task<Result<ServiceRequestDto>> TechnicianTransitionAsync(
        int id, string userId, RequestStatus to,
        Func<ServiceRequest, string?>? validate = null,
        Action<ServiceRequest>? apply = null)
    {
        var load = await LoadForTechnicianAsync(id, userId);
        if (!load.Succeeded)
            return Result<ServiceRequestDto>.Fail(load.Error!, load.ErrorType);

        var request = load.Data!;

        if (!RequestStateMachine.CanTransition(request.Status, to))
            return Result<ServiceRequestDto>.Fail($"Cannot change status from '{request.Status}' to '{to}'.");

        var error = validate?.Invoke(request);
        if (error is not null)
            return Result<ServiceRequestDto>.Fail(error);

        request.Status = to;
        request.UpdatedAt = DateTime.UtcNow;
        apply?.Invoke(request);

        await _db.SaveChangesAsync();
        return Result<ServiceRequestDto>.Ok(request.ToDto());
    }

    private async Task<Result<ServiceRequest>> LoadForTechnicianAsync(int id, string userId)
    {
        var techId = await GetTechnicianProfileIdAsync(userId);
        if (techId is null)
            return Result<ServiceRequest>.Fail("You have not created your technician profile yet.", ErrorType.Forbidden);

        var request = await _db.ServiceRequests.WithDetails().FirstOrDefaultAsync(r => r.Id == id);
        if (request is null)
            return Result<ServiceRequest>.Fail("Request not found.", ErrorType.NotFound);

        if (request.TechnicianProfileId != techId)
            return Result<ServiceRequest>.Fail("This request is not assigned to you.", ErrorType.Forbidden);

        return Result<ServiceRequest>.Ok(request);
    }

    private async Task<int?> GetTechnicianProfileIdAsync(string userId)
    {
        return await _db.TechnicianProfiles
            .Where(t => t.UserId == userId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<ServiceRequestDto> GetDtoAsync(int id)
    {
        var fresh = await _db.ServiceRequests.AsNoTracking().WithDetails().FirstAsync(r => r.Id == id);
        return fresh.ToDto();
    }
}