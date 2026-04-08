using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class AIReviewResult
{
    [Key]
    public Guid ReviewId { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }

    [Required][MaxLength(50)]
    public string ReviewStatus { get; set; } = "Pending";

    public string? ComparisonSummary { get; set; } // JSON
    public string? IdentifiedFlags { get; set; } // JSON
    public string? GeneratedEmailDraft { get; set; }
    public DateTime? ReviewedDate { get; set; }

    [MaxLength(200)]
    public string? ReviewedBy { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Application? Application { get; set; }
    public ICollection<Flag> Flags { get; set; } = new List<Flag>();
}
