namespace FAid.API.DTOs;

public record AIReviewResultDto(
    Guid ReviewId,
    Guid ApplicationId,
    string ReviewStatus,
    string? ComparisonSummary,
    string? IdentifiedFlags,
    string? GeneratedEmailDraft,
    DateTime? ReviewedDate,
    string? ReviewedBy,
    DateTime CreatedDate,
    IEnumerable<FlagDto> Flags
);

public record FlagDto(
    Guid FlagId,
    Guid ReviewId,
    string FlagType,
    string Title,
    string Description,
    string Severity,
    string? SourceField,
    decimal? DiscrepancyAmount,
    DateTime CreatedDate
);

public record SendEmailDto(
    string RecipientEmail,
    string? CustomMessage
);

public record ApproveReviewDto(
    string ReviewedBy
);
