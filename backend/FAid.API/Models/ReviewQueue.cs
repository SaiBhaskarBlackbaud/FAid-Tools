using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class ReviewQueue
{
    [Key]
    public Guid QueueId { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }

    [Required][MaxLength(50)]
    public string QueueStatus { get; set; } = "Pending";

    [MaxLength(200)]
    public string? AssignedTo { get; set; }

    public int DaysInQueue { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedDate { get; set; }

    public Application? Application { get; set; }
}
