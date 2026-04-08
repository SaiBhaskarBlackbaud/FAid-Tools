using System.ComponentModel.DataAnnotations;

namespace FAid.API.Models;

public class Document
{
    [Key]
    public Guid DocumentId { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }

    [Required][MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty;

    [Required][MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? S3KeyOrPath { get; set; }

    [Required][MaxLength(50)]
    public string DocumentStatus { get; set; } = "Uploaded";

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Application? Application { get; set; }
}
