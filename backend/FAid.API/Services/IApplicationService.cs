using FAid.API.DTOs;

namespace FAid.API.Services;

public interface IApplicationService
{
    Task<IEnumerable<ApplicationSummaryDto>> GetAllAsync(int page, int pageSize);
    Task<ApplicationDetailDto?> GetByIdAsync(Guid id);
    Task<ApplicationSummaryDto> CreateAsync(CreateApplicationDto dto);
    Task<ApplicationSummaryDto?> UpdateAsync(Guid id, UpdateApplicationDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync();
}
