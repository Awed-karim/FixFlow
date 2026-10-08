using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Technicians;
using FixFlow.Domain.Entities;
using FixFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Services;

public class TechnicianService : ITechnicianService
{
    private readonly AppDbContext _db;

    public TechnicianService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TechnicianDto>> GetAllAsync(int? categoryId, bool? onlyAvailable)
    {
        var query = _db.TechnicianProfiles
            .AsNoTracking()
            .Include(t => t.ServiceCategory)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.ServiceCategoryId == categoryId.Value);

        if (onlyAvailable == true)
            query = query.Where(t => t.IsAvailable);

        var list = await query.OrderBy(t => t.FullName).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<Result<TechnicianDto>> GetByIdAsync(int id)
    {
        var tech = await _db.TechnicianProfiles
            .AsNoTracking()
            .Include(t => t.ServiceCategory)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tech is null)
            return Result<TechnicianDto>.Fail("Technician not found.", ErrorType.NotFound);

        return Result<TechnicianDto>.Ok(ToDto(tech));
    }

    public async Task<Result<TechnicianDto>> GetMyProfileAsync(string userId)
    {
        var tech = await _db.TechnicianProfiles
            .AsNoTracking()
            .Include(t => t.ServiceCategory)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (tech is null)
            return Result<TechnicianDto>.Fail("You have not created your technician profile yet.", ErrorType.NotFound);

        return Result<TechnicianDto>.Ok(ToDto(tech));
    }

    public async Task<Result<TechnicianDto>> UpsertMyProfileAsync(string userId, UpsertTechnicianProfileRequest request)
    {
        if (!await _db.ServiceCategories.AnyAsync(c => c.Id == request.ServiceCategoryId))
            return Result<TechnicianDto>.Fail("Service category not found.", ErrorType.Validation);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Result<TechnicianDto>.Fail("User not found.", ErrorType.NotFound);

        var profile = await _db.TechnicianProfiles.FirstOrDefaultAsync(t => t.UserId == userId);

        if (profile is null)
        {
            profile = new TechnicianProfile { UserId = userId };
            _db.TechnicianProfiles.Add(profile);
        }
        else
        {
            profile.UpdatedAt = DateTime.UtcNow;
        }

        profile.FullName = user.FullName;
        profile.PhoneNumber = user.PhoneNumber ?? string.Empty;
        profile.ServiceCategoryId = request.ServiceCategoryId;
        profile.Latitude = request.Latitude;
        profile.Longitude = request.Longitude;
        profile.IsAvailable = request.IsAvailable;

        await _db.SaveChangesAsync();
        await _db.Entry(profile).Reference(p => p.ServiceCategory).LoadAsync();

        return Result<TechnicianDto>.Ok(ToDto(profile));
    }

    public async Task<Result<TechnicianDto>> SetAvailabilityAsync(string userId, bool isAvailable)
    {
        var profile = await _db.TechnicianProfiles
            .Include(t => t.ServiceCategory)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (profile is null)
            return Result<TechnicianDto>.Fail("You have not created your technician profile yet.", ErrorType.NotFound);

        profile.IsAvailable = isAvailable;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Result<TechnicianDto>.Ok(ToDto(profile));
    }

    private static TechnicianDto ToDto(TechnicianProfile t) => new()
    {
        Id = t.Id,
        UserId = t.UserId,
        FullName = t.FullName,
        PhoneNumber = t.PhoneNumber,
        ServiceCategoryId = t.ServiceCategoryId,
        ServiceCategoryName = t.ServiceCategory?.Name ?? string.Empty,
        Latitude = t.Latitude,
        Longitude = t.Longitude,
        IsAvailable = t.IsAvailable,
        AverageRating = t.AverageRating,
        CompletedJobsCount = t.CompletedJobsCount
    };
}