using FixFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.API.Controllers;

[Authorize]
[Route("api/requests/{id:int}/share")]
public class ShareController : ApiControllerBase
{
    private readonly IServiceRequestService _service;

    public ShareController(IServiceRequestService service)
    {
        _service = service;
    }

    // رابط التتبع + رابط واتساب جاهز للمشاركة بزرار واحد
    [HttpGet]
    public async Task<IActionResult> Get(int id)
        => FromResult(await _service.GetShareLinksAsync(id, UserId, UserRole));
}