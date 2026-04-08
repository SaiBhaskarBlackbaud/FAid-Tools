namespace FAid.API.DTOs;

public record QueueItemDto(
    Guid QueueId,
    Guid ApplicationId,
    string FamilyName,
    string ApplicationNumber,
    string QueueStatus,
    string? AssignedTo,
    int DaysInQueue,
    int Priority,
    DateTime CreatedDate,
    DateTime? CompletedDate
);

public record UpdateQueueDto(
    string? QueueStatus,
    string? AssignedTo,
    int? Priority
);
