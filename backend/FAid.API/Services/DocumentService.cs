using Microsoft.EntityFrameworkCore;
using FAid.API.Data;
using FAid.API.DTOs;
using FAid.API.Models;

namespace FAid.API.Services;

public class DocumentService : IDocumentService
{
    private readonly FAidDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(FAidDbContext context, IWebHostEnvironment env, ILogger<DocumentService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<IEnumerable<DocumentDto>> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _context.Documents
            .Where(d => d.ApplicationId == applicationId)
            .Select(d => new DocumentDto(
                d.DocumentId, d.ApplicationId, d.DocumentType, d.FileName,
                d.FileSize, d.UploadedDate, d.DocumentStatus, d.CreatedDate))
            .ToListAsync();
    }

    public async Task<DocumentDto> UploadAsync(Guid applicationId, string documentType, IFormFile file)
    {
        var uploadsPath = Path.Combine(_env.ContentRootPath, "uploads", applicationId.ToString());
        Directory.CreateDirectory(uploadsPath);

        var safeFileName = Path.GetFileName(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
        var filePath = Path.Combine(uploadsPath, uniqueFileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        var document = new Document
        {
            ApplicationId = applicationId,
            DocumentType = documentType,
            FileName = safeFileName,
            FileSize = file.Length,
            S3KeyOrPath = filePath,
            DocumentStatus = "Uploaded"
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document {DocumentId} uploaded for application {ApplicationId}", document.DocumentId, applicationId);

        return new DocumentDto(
            document.DocumentId, document.ApplicationId, document.DocumentType,
            document.FileName, document.FileSize, document.UploadedDate,
            document.DocumentStatus, document.CreatedDate);
    }

    public async Task<bool> DeleteAsync(Guid documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null) return false;

        if (document.S3KeyOrPath != null && File.Exists(document.S3KeyOrPath))
        {
            File.Delete(document.S3KeyOrPath);
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(byte[] Content, string FileName)?> DownloadAsync(Guid documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.S3KeyOrPath == null) return null;
        if (!File.Exists(document.S3KeyOrPath)) return null;

        var content = await File.ReadAllBytesAsync(document.S3KeyOrPath);
        return (content, document.FileName);
    }
}
