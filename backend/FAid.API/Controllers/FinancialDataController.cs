using Microsoft.AspNetCore.Mvc;
using FAid.API.DTOs;
using FAid.API.Services;

namespace FAid.API.Controllers;

[ApiController]
[Route("api/financial-data")]
public class FinancialDataController : ControllerBase
{
    private readonly IFinancialDataService _service;

    public FinancialDataController(IFinancialDataService service) => _service = service;

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<IEnumerable<FinancialDataDto>>> GetByApplication(Guid applicationId)
    {
        var data = await _service.GetByApplicationIdAsync(applicationId);
        return Ok(data);
    }

    [HttpPost]
    public async Task<ActionResult<FinancialDataDto>> Create([FromBody] CreateFinancialDataDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetByApplication), new { applicationId = result.ApplicationId }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<FinancialDataDto>> Update(Guid id, [FromBody] UpdateFinancialDataDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return result == null ? NotFound() : Ok(result);
    }
}
