namespace FAid.API.DTOs;

public record DocumentDto(
    Guid DocumentId,
    Guid ApplicationId,
    string DocumentType,
    string FileName,
    long FileSize,
    DateTime UploadedDate,
    string DocumentStatus,
    DateTime CreatedDate
);

public record DocumentUploadDto(
    Guid ApplicationId,
    string DocumentType
);
