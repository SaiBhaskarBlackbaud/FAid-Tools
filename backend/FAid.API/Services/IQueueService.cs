using FAid.API.DTOs;

namespace FAid.API.Services;

public interface IQueueService
{
    Task<IEnumerable<QueueItemDto>> GetQueueAsync();
    Task<QueueItemDto?> UpdateQueueItemAsync(Guid queueId, UpdateQueueDto dto);
    Task<DashboardMetricsDto> GetMetricsAsync();
    Task<QueueSummaryDto> GetQueueSummaryAsync();
}
