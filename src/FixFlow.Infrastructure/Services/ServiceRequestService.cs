using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Requests;
using FixFlow.Domain.Entities;
using FixFlow.Domain.Enums;
using FixFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Services;

public class ServiceRequestService : IServiceRequestService
{
    private readonly AppDbContext _db;

    public ServiceRequestService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ServiceRequestDto>> CreateAsync(string customerId, CreateServiceRequestRequest request)
    {
        var category = await _db.ServiceCategories.FirstOrDefaultAsync(c => c.Id == request.ServiceCategoryId);
        if (category is null)
            return Result<ServiceRequestDto>.Fail("Service category not found.", ErrorType.Validation);

        var entity = new ServiceRequest
        {
            CustomerId = customerId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ServiceCategoryId = category.Id,
            ServiceCategory = category,
            Status = RequestStatus.Pending
        };

        _db.ServiceRequests.Add(entity);
        await _db.SaveChangesAsync();

        return Result<ServiceRequestDto>.Ok(ToDto(entity));
    }

    public async Task<Result<PagedResult<ServiceRequestDto>>> GetAllAsync(
        string userId, string role, string? status, int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 50);

        IQueryable<ServiceRequest> query = _db.ServiceRequests.AsNoTracking();

        if (role == AppRolesNames.Customer)
        {
            query = query.Where(r => r.CustomerId == userId);
        }
        else if (role == AppRolesNames.Technician)
        {
            var techId = await GetTechnicianProfileIdAsync(userId);
            if (techId is null)
                return Result<PagedResult<ServiceRequestDto>>.Ok(new PagedResult<ServiceRequestDto>
                {
                    Page = page,
                    PageSize = pageSize
                });

            query = query.Where(r => r.TechnicianProfileId == techId);
        }
        else if (role != AppRolesNames.Admin)
        {
            return Result<PagedResult<ServiceRequestDto>>.Fail("Forbidden.", ErrorType.Forbidden);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<RequestStatus>(status, true, out var parsed))
                return Result<PagedResult<ServiceRequestDto>>.Fail(
                    $"Invalid status. Allowed: {string.Join(", ", Enum.GetNames<RequestStatus>())}",
                    ErrorType.Validation);

            query = query.Where(r => r.Status == parsed);
        }

        var total = await query.CountAsync();

        var items = await WithDetails(query)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<PagedResult<ServiceRequestDto>>.Ok(new PagedResult<ServiceRequestDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        });
    }

    public async Task<Result<ServiceRequestDto>> GetByIdAsync(int id, string userId, string role)
    {
        var request = await WithDetails(_db.ServiceRequests.AsNoTracking())
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
            return Result<ServiceRequestDto>.Fail("Request not found.", ErrorType.NotFound);

        var allowed = role == AppRolesNames.Admin
                      || (role == AppRolesNames.Customer && request.CustomerId == userId);

        if (!allowed && role == AppRolesNames.Technician)
        {
            var techId = await GetTechnicianProfileIdAsync(userId);
            allowed = techId is not null && request.TechnicianProfileId == techId;
        }

        if (!allowed)
            return Result<ServiceRequestDto>.Fail("You are not allowed to view this request.", ErrorType.Forbidden);

        return Result<ServiceRequestDto>.Ok(ToDto(request));
    }

    public async Task<Result<ServiceRequestDto>> CancelAsync(int id, string userId, string role)
    {
        var request = await WithDetails(_db.ServiceRequests).FirstOrDefaultAsync(r => r.Id == id);

        if (request is null)
            return Result<ServiceRequestDto>.Fail("Request not found.", ErrorType.NotFound);

        var allowed = role == AppRolesNames.Admin
                      || (role == AppRolesNames.Customer && request.CustomerId == userId);

        if (!allowed)
            return Result<ServiceRequestDto>.Fail("You are not allowed to cancel this request.", ErrorType.Forbidden);

        var cancellable = request.Status is RequestStatus.Pending
                          or RequestStatus.Assigned
                          or RequestStatus.Accepted;

        if (!cancellable)
            return Result<ServiceRequestDto>.Fail(
                $"A request with status '{request.Status}' can no longer be cancelled.",
                ErrorType.Validation);

        request.Status = RequestStatus.Cancelled;
        request.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Result<ServiceRequestDto>.Ok(ToDto(request));
    }

    public async Task<Result<TrackingDto>> TrackAsync(Guid token)
    {
        var request = await WithDetails(_db.ServiceRequests.AsNoTracking())
            .FirstOrDefaultAsync(r => r.TrackingToken == token);

        if (request is null)
            return Result<TrackingDto>.Fail("Tracking link is invalid.", ErrorType.NotFound);

        return Result<TrackingDto>.Ok(new TrackingDto
        {
            Title = request.Title,
            Status = request.Status.ToString(),
            ServiceCategoryName = request.ServiceCategory.Name,
            TechnicianName = request.TechnicianProfile?.FullName,
            CreatedAt = request.CreatedAt,
            AssignedAt = request.AssignedAt,
            CompletedAt = request.CompletedAt,
            Photos = request.Photos
                .Where(p => p.Type != PhotoType.Problem)
                .Select(p => new PhotoDto { Url = p.Url, Type = p.Type.ToString() })
                .ToList()
        });
    }

    // ---------- Helpers ----------

    private async Task<int?> GetTechnicianProfileIdAsync(string userId)
    {
        return await _db.TechnicianProfiles
            .Where(t => t.UserId == userId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();
    }

    private static IQueryable<ServiceRequest> WithDetails(IQueryable<ServiceRequest> query)
    {
        return query
            .Include(r => r.ServiceCategory)
            .Include(r => r.TechnicianProfile)
            .Include(r => r.Photos);
    }

    private static ServiceRequestDto ToDto(ServiceRequest r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        Address = r.Address,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        Status = r.Status.ToString(),
        ServiceCategoryId = r.ServiceCategoryId,
        ServiceCategoryName = r.ServiceCategory?.Name ?? string.Empty,
        TechnicianProfileId = r.TechnicianProfileId,
        TechnicianName = r.TechnicianProfile?.FullName,
        TechnicianPhone = r.TechnicianProfile?.PhoneNumber,
        TrackingToken = r.TrackingToken,
        CreatedAt = r.CreatedAt,
        AssignedAt = r.AssignedAt,
        CompletedAt = r.CompletedAt,
        Photos = r.Photos
            .Select(p => new PhotoDto { Url = p.Url, Type = p.Type.ToString() })
            .ToList()
    };
}

// أسماء الأدوار (نفس قيم AppRoles في Application)
internal static class AppRolesNames
{
    public const string Customer = FixFlow.Application.Common.AppRoles.Customer;
    public const string Technician = FixFlow.Application.Common.AppRoles.Technician;
    public const string Admin = FixFlow.Application.Common.AppRoles.Admin;
}