using FixFlow.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FixFlow.API.Realtime;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<UpdatesHub> _hub;

    public SignalRRealtimeNotifier(IHubContext<UpdatesHub> hub)
    {
        _hub = hub;
    }

    public Task SendToUserAsync(string userId, string eventName, object payload)
        => _hub.Clients.Group(UpdatesHub.UserGroup(userId)).SendAsync(eventName, payload);

    public Task SendToAdminsAsync(string eventName, object payload)
        => _hub.Clients.Group(UpdatesHub.AdminsGroup).SendAsync(eventName, payload);

    public Task SendToTrackingAsync(Guid trackingToken, string eventName, object payload)
        => _hub.Clients.Group(UpdatesHub.TrackingGroup(trackingToken)).SendAsync(eventName, payload);
}