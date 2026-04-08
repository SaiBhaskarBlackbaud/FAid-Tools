using Microsoft.AspNetCore.Mvc;
using FAid.API.DTOs;
using FAid.API.Services;

namespace FAid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _service;

    public ApplicationsController(IApplicationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationSummaryDto>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var items = await _service.GetAllAsync(page, pageSize);
        var total = await _service.GetTotalCountAsync();
        Response.Headers.Append("X-Total-Count", total.ToString());
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApplicationDetailDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationSummaryDto>> Create([FromBody] CreateApplicationDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.ApplicationId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApplicationSummaryDto>> Update(Guid id, [FromBody] UpdateApplicationDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("queue")]
    public async Task<ActionResult<IEnumerable<QueueItemDto>>> GetQueue([FromServices] IQueueService queueService)
    {
        var items = await queueService.GetQueueAsync();
        return Ok(items);
    }
}
