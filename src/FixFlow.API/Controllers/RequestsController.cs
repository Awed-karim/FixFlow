using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.API.Controllers;

[Authorize]
[Route("api/requests")]
public class RequestsController : ApiControllerBase
{
    private readonly IServiceRequestService _service;

    public RequestsController(IServiceRequestService service)
    {
        _service = service;
    }

    // العميل بس ينشئ طلب
    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceRequestRequest request)
    {
        var result = await _service.CreateAsync(UserId, request);
        if (!result.Succeeded)
            return ErrorResponse(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    // العميل: طلباته | الفني: الطلبات المعينة له | الأدمن: الكل
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        => FromResult(await _service.GetAllAsync(UserId, UserRole, status, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => FromResult(await _service.GetByIdAsync(id, UserId, UserRole));

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
        => FromResult(await _service.CancelAsync(id, UserId, UserRole));

    // رابط التتبع (Quick Share): بدون تسجيل دخول
    [AllowAnonymous]
    [HttpGet("track/{token:guid}")]
    public async Task<IActionResult> Track(Guid token)
        => FromResult(await _service.TrackAsync(token));
}