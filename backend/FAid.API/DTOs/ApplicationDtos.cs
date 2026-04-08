namespace FAid.API.DTOs;

public record CreateApplicationDto(
    string FamilyName,
    int Grade,
    int DependentCount,
    DateTime SubmissionDate,
    string ApplicationNumber
);

public record UpdateApplicationDto(
    string? FamilyName,
    int? Grade,
    int? DependentCount,
    string? Status
);

public record ApplicationSummaryDto(
    Guid ApplicationId,
    string FamilyName,
    int Grade,
    int DependentCount,
    string ApplicationNumber,
    string Status,
    DateTime SubmissionDate,
    DateTime CreatedDate
);

public record ApplicationDetailDto(
    Guid ApplicationId,
    string FamilyName,
    int Grade,
    int DependentCount,
    string ApplicationNumber,
    string Status,
    DateTime SubmissionDate,
    DateTime CreatedDate,
    DateTime LastModifiedDate,
    IEnumerable<ApplicantDto> Applicants,
    IEnumerable<DocumentDto> Documents,
    IEnumerable<FinancialDataDto> FinancialData,
    AIReviewResultDto? AIReviewResult,
    QueueItemDto? QueueItem
);

public record ApplicantDto(
    Guid ApplicantId,
    Guid ApplicationId,
    string FirstName,
    string LastName,
    string EmailAddress,
    string RelationshipToFamily,
    DateTime CreatedDate
);

public record CreateApplicantDto(
    Guid ApplicationId,
    string FirstName,
    string LastName,
    string EmailAddress,
    string RelationshipToFamily
);
