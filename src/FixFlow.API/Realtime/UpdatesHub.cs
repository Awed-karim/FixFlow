using FixFlow.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace FixFlow.API.Realtime;

public class UpdatesHub : Hub
{
    public const string AdminsGroup = "admins";

    public static string UserGroup(string userId) => $"user-{userId}";
    public static string TrackingGroup(Guid token) => $"track-{token:N}";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;

        if (user?.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
                await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

            if (user.FindFirst("role")?.Value == AppRoles.Admin)
                await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }

        await base.OnConnectedAsync();
    }

    // صفحة التتبع العامة (Quick Share): أي حد معاه الـ token يتابع الطلب لحظياً
    public Task JoinTracking(Guid token)
        => Groups.AddToGroupAsync(Context.ConnectionId, TrackingGroup(token));
}