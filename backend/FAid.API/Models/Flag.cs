using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class Flag
{
    [Key]
    public Guid FlagId { get; set; } = Guid.NewGuid();
    public Guid ReviewId { get; set; }

    [Required][MaxLength(50)]
    public string FlagType { get; set; } = string.Empty;

    [Required][MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required][MaxLength(50)]
    public string Severity { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? SourceField { get; set; }

    public decimal? DiscrepancyAmount { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public AIReviewResult? AIReviewResult { get; set; }
}
