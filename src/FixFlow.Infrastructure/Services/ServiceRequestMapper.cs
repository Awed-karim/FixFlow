using FixFlow.Application.Requests;
using FixFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Infrastructure.Services;

internal static class ServiceRequestMapper
{
    public static IQueryable<ServiceRequest> WithDetails(this IQueryable<ServiceRequest> query)
    {
        return query
            .Include(r => r.ServiceCategory)
            .Include(r => r.TechnicianProfile)
            .Include(r => r.Photos);
    }

    public static ServiceRequestDto ToDto(this ServiceRequest r) => new()
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