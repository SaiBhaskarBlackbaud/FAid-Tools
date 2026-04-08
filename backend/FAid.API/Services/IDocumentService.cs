using FAid.API.DTOs;

namespace FAid.API.Services;

public interface IDocumentService
{
    Task<IEnumerable<DocumentDto>> GetByApplicationIdAsync(Guid applicationId);
    Task<DocumentDto> UploadAsync(Guid applicationId, string documentType, IFormFile file);
    Task<bool> DeleteAsync(Guid documentId);
    Task<(byte[] Content, string FileName)?> DownloadAsync(Guid documentId);
}
