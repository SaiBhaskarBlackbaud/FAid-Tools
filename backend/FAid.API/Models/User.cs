using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class User
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();

    [Required][MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required][MaxLength(255)]
    public string EmailAddress { get; set; } = string.Empty;

    [Required][MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required][MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required][MaxLength(50)]
    public string Role { get; set; } = "Reviewer";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginDate { get; set; }
}
