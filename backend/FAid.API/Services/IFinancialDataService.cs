using FAid.API.DTOs;

namespace FAid.API.Services;

public interface IFinancialDataService
{
    Task<IEnumerable<FinancialDataDto>> GetByApplicationIdAsync(Guid applicationId);
    Task<FinancialDataDto> CreateAsync(CreateFinancialDataDto dto);
    Task<FinancialDataDto?> UpdateAsync(Guid id, UpdateFinancialDataDto dto);
}
