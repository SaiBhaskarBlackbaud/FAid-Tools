using Microsoft.AspNetCore.Mvc;
using FAid.API.DTOs;
using FAid.API.Services;

namespace FAid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IQueueService _service;

    public DashboardController(IQueueService service) => _service = service;

    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics()
    {
        var metrics = await _service.GetMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("queue-summary")]
    public async Task<ActionResult<QueueSummaryDto>> GetQueueSummary()
    {
        var summary = await _service.GetQueueSummaryAsync();
        return Ok(summary);
    }
}
