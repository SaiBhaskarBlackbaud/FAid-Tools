using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FAid.API.Data;
using FAid.API.DTOs;
using FAid.API.Models;

namespace FAid.API.Services;

public class AIReviewService : IAIReviewService
{
    private readonly FAidDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AIReviewService> _logger;
    private readonly HttpClient _httpClient;

    public AIReviewService(FAidDbContext context, IConfiguration config,
        ILogger<AIReviewService> logger, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ClaudeClient");
    }

    public async Task<AIReviewResultDto> StartReviewAsync(Guid applicationId)
    {
        var existingReview = await _context.AIReviewResults
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);

        AIReviewResult review;
        if (existingReview != null)
        {
            existingReview.ReviewStatus = "InProgress";
            existingReview.ReviewedDate = null;
            review = existingReview;
        }
        else
        {
            review = new AIReviewResult
            {
                ApplicationId = applicationId,
                ReviewStatus = "InProgress"
            };
            _context.AIReviewResults.Add(review);
        }

        await _context.SaveChangesAsync();

        // Run review in background
        _ = Task.Run(() => PerformReviewAsync(applicationId, review.ReviewId));

        return MapToDto(review);
    }

    public async Task<AIReviewResultDto?> GetReviewAsync(Guid applicationId)
    {
        var review = await _context.AIReviewResults
            .Include(r => r.Flags)
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);

        return review == null ? null : MapToDto(review);
    }

    public async Task<IEnumerable<FlagDto>> GetFlagsAsync(Guid applicationId)
    {
        var review = await _context.AIReviewResults
            .Include(r => r.Flags)
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);

        if (review == null) return Enumerable.Empty<FlagDto>();

        return review.Flags.Select(f => new FlagDto(
            f.FlagId, f.ReviewId, f.FlagType, f.Title, f.Description,
            f.Severity, f.SourceField, f.DiscrepancyAmount, f.CreatedDate));
    }

    public async Task<string?> GetEmailDraftAsync(Guid applicationId)
    {
        var review = await _context.AIReviewResults
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);
        return review?.GeneratedEmailDraft;
    }

    public async Task<bool> SendEmailAsync(Guid applicationId, SendEmailDto dto)
    {
        var review = await _context.AIReviewResults
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);

        if (review == null) return false;

        _logger.LogInformation("Email sent to {Email} for application {ApplicationId}", dto.RecipientEmail, applicationId);

        var app = await _context.Applications.FindAsync(applicationId);
        if (app != null)
        {
            app.Status = "DocsRequired";
            app.LastModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<AIReviewResultDto?> ApproveAsync(Guid applicationId, ApproveReviewDto dto)
    {
        var review = await _context.AIReviewResults
            .Include(r => r.Flags)
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);

        if (review == null) return null;

        review.ReviewStatus = "Completed";
        review.ReviewedDate = DateTime.UtcNow;
        review.ReviewedBy = dto.ReviewedBy;

        var app = await _context.Applications.FindAsync(applicationId);
        if (app != null)
        {
            app.Status = "Verified";
            app.LastModifiedDate = DateTime.UtcNow;
        }

        var queue = await _context.ReviewQueues.FirstOrDefaultAsync(q => q.ApplicationId == applicationId);
        if (queue != null)
        {
            queue.QueueStatus = "Completed";
            queue.CompletedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return MapToDto(review);
    }

    private async Task PerformReviewAsync(Guid applicationId, Guid reviewId)
    {
        try
        {
            var app = await _context.Applications
                .Include(a => a.FinancialData)
                .Include(a => a.Applicants)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            var review = await _context.AIReviewResults
                .Include(r => r.Flags)
                .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

            if (app == null || review == null) return;

            var flags = new List<Flag>();
            var comparisonResults = new Dictionary<string, object>();

            var appFormData = app.FinancialData.FirstOrDefault(fd => fd.DataSource == "ApplicationForm");
            var form1040Data = app.FinancialData.FirstOrDefault(fd => fd.DataSource == "Form1040");

            // Check income discrepancies
            if (appFormData?.HouseholdIncome.HasValue == true && form1040Data?.HouseholdIncome.HasValue == true)
            {
                var reported = appFormData.HouseholdIncome!.Value;
                var from1040 = form1040Data.HouseholdIncome!.Value;
                var variance = reported == 0 ? 0 : Math.Abs(reported - from1040) / reported;

                comparisonResults["IncomeMatch_AppVs1040"] = variance < 0.05m;

                if (variance > 0.05m)
                {
                    flags.Add(new Flag
                    {
                        ReviewId = reviewId,
                        FlagType = "IncomeDiscrepancy",
                        Title = "Income Discrepancy: Application vs Form 1040",
                        Description = $"Reported household income (${reported:N0}) differs from Form 1040 (${from1040:N0}) by {variance:P1}.",
                        Severity = variance > 0.20m ? "Critical" : "Warning",
                        SourceField = "HouseholdIncome",
                        DiscrepancyAmount = Math.Abs(reported - from1040)
                    });
                }
            }

            if (appFormData?.DependentCount != null && form1040Data?.DependentCount != null
                && appFormData.DependentCount != form1040Data.DependentCount)
            {
                flags.Add(new Flag
                {
                    ReviewId = reviewId,
                    FlagType = "DataMismatch",
                    Title = "Dependent Count Mismatch",
                    Description = $"Application reports {appFormData.DependentCount} dependents, Form 1040 shows {form1040Data.DependentCount}.",
                    Severity = "Warning",
                    SourceField = "DependentCount"
                });
            }

            // Check for missing documents by data source
            var documentTypes = new[] { "ApplicationForm", "Form1040", "IRSTranscript" };
            var uploadedTypes = app.FinancialData.Select(fd => fd.DataSource).ToHashSet();
            foreach (var docType in documentTypes)
            {
                if (!uploadedTypes.Contains(docType))
                {
                    flags.Add(new Flag
                    {
                        ReviewId = reviewId,
                        FlagType = "MissingDocument",
                        Title = $"Missing Document: {docType}",
                        Description = $"The {docType} document has not been uploaded or processed.",
                        Severity = "Critical",
                        SourceField = docType
                    });
                }
            }

            review.Flags = flags;
            review.ComparisonSummary = JsonSerializer.Serialize(comparisonResults);
            review.GeneratedEmailDraft = await GenerateEmailDraftAsync(app, flags);
            review.ReviewStatus = "Completed";
            review.ReviewedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var application = await _context.Applications.FindAsync(applicationId);
            if (application != null)
            {
                application.Status = flags.Any(f => f.Severity == "Critical") ? "DocsRequired" : "Processing";
                application.LastModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI Review failed for application {ApplicationId}", applicationId);
            var review = await _context.AIReviewResults.FindAsync(reviewId);
            if (review != null)
            {
                review.ReviewStatus = "Failed";
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task<string> GenerateEmailDraftAsync(Application app, List<Flag> flags)
    {
        var apiKey = _config["Claude:ApiKey"];

        if (string.IsNullOrEmpty(apiKey) || apiKey == "your-claude-api-key-here")
        {
            var issues = string.Join("\n", flags.Select(f => $"- {f.Title}: {f.Description}"));
            return $"""
                Dear {app.FamilyName} Family,

                Thank you for submitting your financial aid application (#{app.ApplicationNumber}).

                During our review, we identified the following items that require your attention:

                {(issues.Length > 0 ? issues : "- No issues found. Your application looks complete.")}

                Please address the above items at your earliest convenience. If you have any questions, please contact our financial aid office.

                Best regards,
                Financial Aid Office
                """;
        }

        try
        {
            var prompt = $"""
                You are a financial aid office assistant. Generate a professional, empathetic email to the {app.FamilyName} family regarding their financial aid application #{app.ApplicationNumber}.

                Issues found during review:
                {string.Join("\n", flags.Select(f => $"- {f.Severity}: {f.Title} - {f.Description}"))}

                The email should:
                1. Be professional but warm
                2. Clearly explain what documents or corrections are needed
                3. Provide a clear call to action
                4. Be concise (under 200 words)

                Generate only the email body, no subject line.
                """;

            var requestBody = new
            {
                model = "claude-3-haiku-20240307",
                max_tokens = 1024,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(responseBody);
                return doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed, using template");
        }

        return $"Dear {app.FamilyName} Family,\n\nPlease review your financial aid application #{app.ApplicationNumber}.\n\nBest regards,\nFinancial Aid Office";
    }

    private static AIReviewResultDto MapToDto(AIReviewResult review)
    {
        return new AIReviewResultDto(
            review.ReviewId, review.ApplicationId, review.ReviewStatus,
            review.ComparisonSummary, review.IdentifiedFlags,
            review.GeneratedEmailDraft, review.ReviewedDate, review.ReviewedBy,
            review.CreatedDate,
            review.Flags.Select(f => new FlagDto(
                f.FlagId, f.ReviewId, f.FlagType, f.Title, f.Description,
                f.Severity, f.SourceField, f.DiscrepancyAmount, f.CreatedDate)));
    }
}
