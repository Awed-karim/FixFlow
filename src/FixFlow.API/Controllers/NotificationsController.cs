using FixFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.API.Controllers;

[Authorize]
[Route("api/notifications")]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
        => Ok(await _service.GetMineAsync(UserId, unreadOnly, page, pageSize));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
        => Ok(new { count = await _service.GetUnreadCountAsync(UserId) });

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
        => FromResult(await _service.MarkAsReadAsync(id, UserId));

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _service.MarkAllAsReadAsync(UserId);
        return NoContent();
    }
}