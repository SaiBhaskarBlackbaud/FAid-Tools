using Microsoft.AspNetCore.Mvc;
using FAid.API.DTOs;
using FAid.API.Services;

namespace FAid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _service;

    public DocumentsController(IDocumentService service) => _service = service;

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<ActionResult<DocumentDto>> Upload(
        [FromForm] DocumentUploadDto dto, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided.");

        var result = await _service.UploadAsync(dto.ApplicationId, dto.DocumentType, file);
        return Ok(result);
    }

    [HttpGet("{applicationId:guid}")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetByApplication(Guid applicationId)
    {
        var docs = await _service.GetByApplicationIdAsync(applicationId);
        return Ok(docs);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var result = await _service.DownloadAsync(id);
        if (result == null) return NotFound();
        return File(result.Value.Content, "application/octet-stream", result.Value.FileName);
    }
}
