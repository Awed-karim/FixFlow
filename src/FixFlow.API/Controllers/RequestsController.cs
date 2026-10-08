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
    private readonly IRequestWorkflowService _workflow;

    public RequestsController(IServiceRequestService service, IRequestWorkflowService workflow)
    {
        _service = service;
        _workflow = workflow;
    }

    // ================= المرحلة 3 =================

    // العميل بس ينشئ طلب (والتعيين التلقائي بيحصل جوه الـ Service)
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

    // ================= المرحلة 4 =================

    // الأدمن: إعادة محاولة التعيين لطلب Pending
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id)
        => FromResult(await _workflow.AssignAsync(id));

    // ---------- الفني ----------

    [Authorize(Roles = AppRoles.Technician)]
    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id)
        => FromResult(await _workflow.AcceptAsync(id, UserId));

    [Authorize(Roles = AppRoles.Technician)]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectRequestRequest request)
        => FromResult(await _workflow.RejectAsync(id, UserId, request.Reason));

    [Authorize(Roles = AppRoles.Technician)]
    [HttpPost("{id:int}/on-the-way")]
    public async Task<IActionResult> OnTheWay(int id)
        => FromResult(await _workflow.StartTravelAsync(id, UserId));

    [Authorize(Roles = AppRoles.Technician)]
    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id)
        => FromResult(await _workflow.StartWorkAsync(id, UserId));

    [Authorize(Roles = AppRoles.Technician)]
    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
        => FromResult(await _workflow.CompleteAsync(id, UserId));

    // ---------- العميل ----------

    [Authorize(Roles = AppRoles.Customer)]
    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, ReviewRequest request)
        => FromResult(await _workflow.ReviewAsync(id, UserId, request));

    // ---------- الصور (عميل أو فني) ----------
    // type = Problem | Before | After

    [Authorize(Roles = AppRoles.Customer + "," + AppRoles.Technician)]
    [HttpPost("{id:int}/photos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> UploadPhoto(int id, [FromQuery] string type, [FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "File is required." });

        await using var stream = file.OpenReadStream();
        var upload = new FileUpload(stream, file.FileName, file.Length, file.ContentType);

        var result = await _workflow.UploadPhotoAsync(id, UserId, UserRole, type, upload);
        if (!result.Succeeded)
            return ErrorResponse(result);

        return Ok(result.Data);
    }
}