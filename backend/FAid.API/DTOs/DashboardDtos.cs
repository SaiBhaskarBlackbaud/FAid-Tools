namespace FAid.API.DTOs;

public record DashboardMetricsDto(
    int TotalApplications,
    int PendingReviews,
    int VerifiedApplications,
    int DocsRequiredApplications,
    int CompletedApplications,
    int InQueueCount
);

public record QueueSummaryDto(
    int TotalInQueue,
    int PendingCount,
    int InProgressCount,
    int OnHoldCount,
    double AverageDaysInQueue,
    IEnumerable<QueueItemDto> RecentItems
);
