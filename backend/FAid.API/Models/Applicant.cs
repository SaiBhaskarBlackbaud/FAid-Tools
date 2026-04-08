using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class Applicant
{
    [Key]
    public Guid ApplicantId { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }

    [Required][MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required][MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required][MaxLength(255)]
    public string EmailAddress { get; set; } = string.Empty;

    [Required][MaxLength(100)]
    public string RelationshipToFamily { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Application? Application { get; set; }
}
