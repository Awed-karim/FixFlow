using FixFlow.Application.Common;
using FixFlow.Application.Requests;

namespace FixFlow.Application.Interfaces;

public interface IRequestWorkflowService
{
    Task<Result<ServiceRequestDto>> AssignAsync(int id);                                   // أدمن
    Task<Result<ServiceRequestDto>> AcceptAsync(int id, string userId);                    // فني
    Task<Result<ServiceRequestDto>> RejectAsync(int id, string userId, string? reason);    // فني
    Task<Result<ServiceRequestDto>> StartTravelAsync(int id, string userId);               // فني: في الطريق
    Task<Result<ServiceRequestDto>> StartWorkAsync(int id, string userId);                 // فني: بدأ الشغل
    Task<Result<ServiceRequestDto>> CompleteAsync(int id, string userId);                  // فني: تم
    Task<Result<ServiceRequestDto>> ReviewAsync(int id, string userId, ReviewRequest request); // عميل
    Task<Result<PhotoDto>> UploadPhotoAsync(int id, string userId, string role, string type, FileUpload file);
}