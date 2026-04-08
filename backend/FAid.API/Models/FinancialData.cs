using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class FinancialData
{
    [Key]
    public Guid FinancialDataId { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }

    [Required][MaxLength(50)]
    public string DataSource { get; set; } = string.Empty;

    public decimal? HouseholdIncome { get; set; }
    public decimal? AGI { get; set; }

    [MaxLength(50)]
    public string? FilingStatus { get; set; }

    public int? DependentCount { get; set; }

    [MaxLength(200)]
    public string? W2Employer1 { get; set; }

    [MaxLength(200)]
    public string? W2Employer2 { get; set; }

    public decimal? OtherIncome { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Application? Application { get; set; }
}
