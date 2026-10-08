using FixFlow.Application.Common;
using FixFlow.Application.Requests;

namespace FixFlow.Application.Interfaces;

public interface IServiceRequestService
{
    Task<Result<ServiceRequestDto>> CreateAsync(string customerId, CreateServiceRequestRequest request);
    Task<Result<PagedResult<ServiceRequestDto>>> GetAllAsync(string userId, string role, string? status, int page, int pageSize);
    Task<Result<ServiceRequestDto>> GetByIdAsync(int id, string userId, string role);
    Task<Result<ServiceRequestDto>> CancelAsync(int id, string userId, string role);
    Task<Result<TrackingDto>> TrackAsync(Guid token);
}