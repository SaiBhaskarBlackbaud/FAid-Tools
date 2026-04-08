using Microsoft.EntityFrameworkCore;
using FAid.API.Data;
using FAid.API.DTOs;
using FAid.API.Models;

namespace FAid.API.Services;

public class ApplicationService : IApplicationService
{
    private readonly FAidDbContext _context;

    public ApplicationService(FAidDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ApplicationSummaryDto>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Applications
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ApplicationSummaryDto(
                a.ApplicationId, a.FamilyName, a.Grade, a.DependentCount,
                a.ApplicationNumber, a.Status, a.SubmissionDate, a.CreatedDate))
            .ToListAsync();
    }

    public async Task<ApplicationDetailDto?> GetByIdAsync(Guid id)
    {
        var app = await _context.Applications
            .Include(a => a.Applicants)
            .Include(a => a.Documents)
            .Include(a => a.FinancialData)
            .Include(a => a.AIReviewResult)
                .ThenInclude(r => r!.Flags)
            .Include(a => a.ReviewQueue)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        if (app == null) return null;

        return MapToDetailDto(app);
    }

    public async Task<ApplicationSummaryDto> CreateAsync(CreateApplicationDto dto)
    {
        var app = new Application
        {
            FamilyName = dto.FamilyName,
            Grade = dto.Grade,
            DependentCount = dto.DependentCount,
            SubmissionDate = dto.SubmissionDate,
            ApplicationNumber = dto.ApplicationNumber,
            Status = "Pending"
        };

        _context.Applications.Add(app);

        // Auto-create queue entry
        _context.ReviewQueues.Add(new ReviewQueue
        {
            ApplicationId = app.ApplicationId,
            QueueStatus = "Pending",
            Priority = 0,
            DaysInQueue = 0
        });

        await _context.SaveChangesAsync();

        return new ApplicationSummaryDto(
            app.ApplicationId, app.FamilyName, app.Grade, app.DependentCount,
            app.ApplicationNumber, app.Status, app.SubmissionDate, app.CreatedDate);
    }

    public async Task<ApplicationSummaryDto?> UpdateAsync(Guid id, UpdateApplicationDto dto)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null) return null;

        if (dto.FamilyName != null) app.FamilyName = dto.FamilyName;
        if (dto.Grade.HasValue) app.Grade = dto.Grade.Value;
        if (dto.DependentCount.HasValue) app.DependentCount = dto.DependentCount.Value;
        if (dto.Status != null) app.Status = dto.Status;
        app.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new ApplicationSummaryDto(
            app.ApplicationId, app.FamilyName, app.Grade, app.DependentCount,
            app.ApplicationNumber, app.Status, app.SubmissionDate, app.CreatedDate);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var app = await _context.Applications.FindAsync(id);
        if (app == null) return false;
        _context.Applications.Remove(app);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Applications.CountAsync();
    }

    private static ApplicationDetailDto MapToDetailDto(Application app)
    {
        return new ApplicationDetailDto(
            app.ApplicationId, app.FamilyName, app.Grade, app.DependentCount,
            app.ApplicationNumber, app.Status, app.SubmissionDate, app.CreatedDate,
            app.LastModifiedDate,
            app.Applicants.Select(a => new ApplicantDto(
                a.ApplicantId, a.ApplicationId, a.FirstName, a.LastName,
                a.EmailAddress, a.RelationshipToFamily, a.CreatedDate)),
            app.Documents.Select(d => new DocumentDto(
                d.DocumentId, d.ApplicationId, d.DocumentType, d.FileName,
                d.FileSize, d.UploadedDate, d.DocumentStatus, d.CreatedDate)),
            app.FinancialData.Select(fd => new FinancialDataDto(
                fd.FinancialDataId, fd.ApplicationId, fd.DataSource, fd.HouseholdIncome,
                fd.AGI, fd.FilingStatus, fd.DependentCount, fd.W2Employer1,
                fd.W2Employer2, fd.OtherIncome, fd.CreatedDate)),
            app.AIReviewResult != null ? new AIReviewResultDto(
                app.AIReviewResult.ReviewId, app.AIReviewResult.ApplicationId,
                app.AIReviewResult.ReviewStatus, app.AIReviewResult.ComparisonSummary,
                app.AIReviewResult.IdentifiedFlags, app.AIReviewResult.GeneratedEmailDraft,
                app.AIReviewResult.ReviewedDate, app.AIReviewResult.ReviewedBy,
                app.AIReviewResult.CreatedDate,
                app.AIReviewResult.Flags.Select(f => new FlagDto(
                    f.FlagId, f.ReviewId, f.FlagType, f.Title, f.Description,
                    f.Severity, f.SourceField, f.DiscrepancyAmount, f.CreatedDate))) : null,
            app.ReviewQueue != null ? new QueueItemDto(
                app.ReviewQueue.QueueId, app.ReviewQueue.ApplicationId,
                app.FamilyName, app.ApplicationNumber, app.ReviewQueue.QueueStatus,
                app.ReviewQueue.AssignedTo, app.ReviewQueue.DaysInQueue,
                app.ReviewQueue.Priority, app.ReviewQueue.CreatedDate,
                app.ReviewQueue.CompletedDate) : null
        );
    }
}
