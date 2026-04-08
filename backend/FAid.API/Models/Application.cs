using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class Application
{
    [Key]
    public Guid ApplicationId { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string FamilyName { get; set; } = string.Empty;

    public int Grade { get; set; }
    public int DependentCount { get; set; }
    public DateTime SubmissionDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string ApplicationNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Applicant> Applicants { get; set; } = new List<Applicant>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<FinancialData> FinancialData { get; set; } = new List<FinancialData>();
    public AIReviewResult? AIReviewResult { get; set; }
    public ReviewQueue? ReviewQueue { get; set; }
}
