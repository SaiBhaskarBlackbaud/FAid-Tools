using Microsoft.AspNetCore.Mvc;
using FAid.API.DTOs;
using FAid.API.Services;

namespace FAid.API.Controllers;

[ApiController]
[Route("api/ai-review")]
public class AIReviewController : ControllerBase
{
    private readonly IAIReviewService _service;

    public AIReviewController(IAIReviewService service) => _service = service;

    [HttpPost("{applicationId:guid}")]
    public async Task<ActionResult<AIReviewResultDto>> StartReview(Guid applicationId)
    {
        var result = await _service.StartReviewAsync(applicationId);
        return Ok(result);
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<AIReviewResultDto>> GetReview(Guid applicationId)
    {
        var result = await _service.GetReviewAsync(applicationId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{applicationId:guid}/flags")]
    public async Task<ActionResult<IEnumerable<FlagDto>>> GetFlags(Guid applicationId)
    {
        var flags = await _service.GetFlagsAsync(applicationId);
        return Ok(flags);
    }

    [HttpGet("{applicationId:guid}/email-draft")]
    public async Task<ActionResult<string>> GetEmailDraft(Guid applicationId)
    {
        var draft = await _service.GetEmailDraftAsync(applicationId);
        return draft == null ? NotFound() : Ok(draft);
    }

    [HttpPost("{applicationId:guid}/send-email")]
    public async Task<IActionResult> SendEmail(Guid applicationId, [FromBody] SendEmailDto dto)
    {
        var success = await _service.SendEmailAsync(applicationId, dto);
        return success ? Ok(new { message = "Email sent successfully" }) : NotFound();
    }

    [HttpPut("{applicationId:guid}/approve")]
    public async Task<ActionResult<AIReviewResultDto>> Approve(Guid applicationId, [FromBody] ApproveReviewDto dto)
    {
        var result = await _service.ApproveAsync(applicationId, dto);
        return result == null ? NotFound() : Ok(result);
    }
}
