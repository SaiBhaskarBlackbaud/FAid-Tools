using FAid.API.DTOs;

namespace FAid.API.Services;

public interface IAIReviewService
{
    Task<AIReviewResultDto> StartReviewAsync(Guid applicationId);
    Task<AIReviewResultDto?> GetReviewAsync(Guid applicationId);
    Task<IEnumerable<FlagDto>> GetFlagsAsync(Guid applicationId);
    Task<string?> GetEmailDraftAsync(Guid applicationId);
    Task<bool> SendEmailAsync(Guid applicationId, SendEmailDto dto);
    Task<AIReviewResultDto?> ApproveAsync(Guid applicationId, ApproveReviewDto dto);
}
