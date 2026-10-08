namespace FixFlow.Application.Interfaces;

public interface IAssignmentService
{
    // بيحاول يعيّن أنسب فني لطلب Pending. بيرجّع true لو نجح.
    Task<bool> TryAssignAsync(int requestId);
}