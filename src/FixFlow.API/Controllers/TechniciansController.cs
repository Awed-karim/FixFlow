using FixFlow.Application.Common;
using FixFlow.Application.Interfaces;
using FixFlow.Application.Technicians;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.API.Controllers;

[Route("api/technicians")]
public class TechniciansController : ApiControllerBase
{
    private readonly ITechnicianService _service;

    public TechniciansController(ITechnicianService service)
    {
        _service = service;
    }

    // ---------- للفني نفسه ----------

    [Authorize(Roles = AppRoles.Technician)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
        => FromResult(await _service.GetMyProfileAsync(UserId));

    // إنشاء أو تعديل البروفايل
    [Authorize(Roles = AppRoles.Technician)]
    [HttpPut("me")]
    public async Task<IActionResult> UpsertMe(UpsertTechnicianProfileRequest request)
        => FromResult(await _service.UpsertMyProfileAsync(UserId, request));

    [Authorize(Roles = AppRoles.Technician)]
    [HttpPut("me/availability")]
    public async Task<IActionResult> SetAvailability(SetAvailabilityRequest request)
        => FromResult(await _service.SetAvailabilityAsync(UserId, request.IsAvailable));

    // ---------- للأدمن ----------

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId, [FromQuery] bool? onlyAvailable)
        => Ok(await _service.GetAllAsync(categoryId, onlyAvailable));

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
        => FromResult(await _service.GetByIdAsync(id));
}