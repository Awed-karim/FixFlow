namespace FixFlow.Application.Interfaces;

public interface IRealtimeNotifier
{
    Task SendToUserAsync(string userId, string eventName, object payload);
    Task SendToAdminsAsync(string eventName, object payload);
    Task SendToTrackingAsync(Guid trackingToken, string eventName, object payload);
}