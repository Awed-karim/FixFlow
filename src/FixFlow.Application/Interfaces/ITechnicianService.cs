using FixFlow.Application.Common;
using FixFlow.Application.Technicians;

namespace FixFlow.Application.Interfaces;

public interface ITechnicianService
{
    Task<List<TechnicianDto>> GetAllAsync(int? categoryId, bool? onlyAvailable);
    Task<Result<TechnicianDto>> GetByIdAsync(int id);
    Task<Result<TechnicianDto>> GetMyProfileAsync(string userId);
    Task<Result<TechnicianDto>> UpsertMyProfileAsync(string userId, UpsertTechnicianProfileRequest request);
    Task<Result<TechnicianDto>> SetAvailabilityAsync(string userId, bool isAvailable);
}