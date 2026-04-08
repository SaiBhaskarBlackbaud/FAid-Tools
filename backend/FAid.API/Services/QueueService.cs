using Microsoft.EntityFrameworkCore;
using FAid.API.Data;
using FAid.API.DTOs;

namespace FAid.API.Services;

public class QueueService : IQueueService
{
    private readonly FAidDbContext _context;

    public QueueService(FAidDbContext context) => _context = context;

    public async Task<IEnumerable<QueueItemDto>> GetQueueAsync()
    {
        return await _context.ReviewQueues
            .Include(q => q.Application)
            .OrderBy(q => q.Priority).ThenBy(q => q.CreatedDate)
            .Select(q => new QueueItemDto(
                q.QueueId, q.ApplicationId,
                q.Application!.FamilyName, q.Application.ApplicationNumber,
                q.QueueStatus, q.AssignedTo, q.DaysInQueue, q.Priority,
                q.CreatedDate, q.CompletedDate))
            .ToListAsync();
    }

    public async Task<QueueItemDto?> UpdateQueueItemAsync(Guid queueId, UpdateQueueDto dto)
    {
        var queue = await _context.ReviewQueues
            .Include(q => q.Application)
            .FirstOrDefaultAsync(q => q.QueueId == queueId);

        if (queue == null) return null;

        if (dto.QueueStatus != null) queue.QueueStatus = dto.QueueStatus;
        if (dto.AssignedTo != null) queue.AssignedTo = dto.AssignedTo;
        if (dto.Priority.HasValue) queue.Priority = dto.Priority.Value;
        if (dto.QueueStatus == "Completed") queue.CompletedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new QueueItemDto(
            queue.QueueId, queue.ApplicationId,
            queue.Application!.FamilyName, queue.Application.ApplicationNumber,
            queue.QueueStatus, queue.AssignedTo, queue.DaysInQueue, queue.Priority,
            queue.CreatedDate, queue.CompletedDate);
    }

    public async Task<DashboardMetricsDto> GetMetricsAsync()
    {
        var counts = await _context.Applications
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = counts.Sum(c => c.Count);
        var pending = counts.FirstOrDefault(c => c.Status == "Pending")?.Count ?? 0;
        var verified = counts.FirstOrDefault(c => c.Status == "Verified")?.Count ?? 0;
        var docsRequired = counts.FirstOrDefault(c => c.Status == "DocsRequired")?.Count ?? 0;
        var completed = counts.FirstOrDefault(c => c.Status == "Completed")?.Count ?? 0;
        var inQueue = await _context.ReviewQueues.CountAsync(q => q.QueueStatus != "Completed");

        return new DashboardMetricsDto(total, pending, verified, docsRequired, completed, inQueue);
    }

    public async Task<QueueSummaryDto> GetQueueSummaryAsync()
    {
        var queues = await _context.ReviewQueues
            .Include(q => q.Application)
            .ToListAsync();

        var total = queues.Count;
        var pendingCount = queues.Count(q => q.QueueStatus == "Pending");
        var inProgressCount = queues.Count(q => q.QueueStatus == "InProgress");
        var onHoldCount = queues.Count(q => q.QueueStatus == "OnHold");
        var avgDays = queues.Any() ? queues.Average(q => q.DaysInQueue) : 0;

        var recentItems = queues
            .OrderByDescending(q => q.CreatedDate)
            .Take(10)
            .Select(q => new QueueItemDto(
                q.QueueId, q.ApplicationId,
                q.Application!.FamilyName, q.Application.ApplicationNumber,
                q.QueueStatus, q.AssignedTo, q.DaysInQueue, q.Priority,
                q.CreatedDate, q.CompletedDate));

        return new QueueSummaryDto(total, pendingCount, inProgressCount, onHoldCount, avgDays, recentItems);
    }
}
