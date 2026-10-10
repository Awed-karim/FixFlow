using FixFlow.Application.Common;

namespace FixFlow.Application.Interfaces;

public interface IRequestEventPublisher
{
    // relatedUserId: مستخدم إضافي يخصه الحدث (مثلاً الفني اللي انتهت مهلته)
    Task PublishAsync(int requestId, RequestEventType type, string? relatedUserId = null);
}