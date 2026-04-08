namespace FAid.API.DTOs;

public record FinancialDataDto(
    Guid FinancialDataId,
    Guid ApplicationId,
    string DataSource,
    decimal? HouseholdIncome,
    decimal? AGI,
    string? FilingStatus,
    int? DependentCount,
    string? W2Employer1,
    string? W2Employer2,
    decimal? OtherIncome,
    DateTime CreatedDate
);

public record CreateFinancialDataDto(
    Guid ApplicationId,
    string DataSource,
    decimal? HouseholdIncome,
    decimal? AGI,
    string? FilingStatus,
    int? DependentCount,
    string? W2Employer1,
    string? W2Employer2,
    decimal? OtherIncome
);

public record UpdateFinancialDataDto(
    decimal? HouseholdIncome,
    decimal? AGI,
    string? FilingStatus,
    int? DependentCount,
    string? W2Employer1,
    string? W2Employer2,
    decimal? OtherIncome
);
