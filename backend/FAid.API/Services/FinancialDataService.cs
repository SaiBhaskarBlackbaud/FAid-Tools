using Microsoft.EntityFrameworkCore;
using FAid.API.Data;
using FAid.API.DTOs;
using FAid.API.Models;

namespace FAid.API.Services;

public class FinancialDataService : IFinancialDataService
{
    private readonly FAidDbContext _context;

    public FinancialDataService(FAidDbContext context) => _context = context;

    public async Task<IEnumerable<FinancialDataDto>> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _context.FinancialData
            .Where(fd => fd.ApplicationId == applicationId)
            .Select(fd => new FinancialDataDto(
                fd.FinancialDataId, fd.ApplicationId, fd.DataSource, fd.HouseholdIncome,
                fd.AGI, fd.FilingStatus, fd.DependentCount, fd.W2Employer1,
                fd.W2Employer2, fd.OtherIncome, fd.CreatedDate))
            .ToListAsync();
    }

    public async Task<FinancialDataDto> CreateAsync(CreateFinancialDataDto dto)
    {
        var fd = new FinancialData
        {
            ApplicationId = dto.ApplicationId,
            DataSource = dto.DataSource,
            HouseholdIncome = dto.HouseholdIncome,
            AGI = dto.AGI,
            FilingStatus = dto.FilingStatus,
            DependentCount = dto.DependentCount,
            W2Employer1 = dto.W2Employer1,
            W2Employer2 = dto.W2Employer2,
            OtherIncome = dto.OtherIncome
        };

        _context.FinancialData.Add(fd);
        await _context.SaveChangesAsync();

        return new FinancialDataDto(
            fd.FinancialDataId, fd.ApplicationId, fd.DataSource, fd.HouseholdIncome,
            fd.AGI, fd.FilingStatus, fd.DependentCount, fd.W2Employer1,
            fd.W2Employer2, fd.OtherIncome, fd.CreatedDate);
    }

    public async Task<FinancialDataDto?> UpdateAsync(Guid id, UpdateFinancialDataDto dto)
    {
        var fd = await _context.FinancialData.FindAsync(id);
        if (fd == null) return null;

        if (dto.HouseholdIncome.HasValue) fd.HouseholdIncome = dto.HouseholdIncome.Value;
        if (dto.AGI.HasValue) fd.AGI = dto.AGI.Value;
        if (dto.FilingStatus != null) fd.FilingStatus = dto.FilingStatus;
        if (dto.DependentCount.HasValue) fd.DependentCount = dto.DependentCount.Value;
        if (dto.W2Employer1 != null) fd.W2Employer1 = dto.W2Employer1;
        if (dto.W2Employer2 != null) fd.W2Employer2 = dto.W2Employer2;
        if (dto.OtherIncome.HasValue) fd.OtherIncome = dto.OtherIncome.Value;

        await _context.SaveChangesAsync();

        return new FinancialDataDto(
            fd.FinancialDataId, fd.ApplicationId, fd.DataSource, fd.HouseholdIncome,
            fd.AGI, fd.FilingStatus, fd.DependentCount, fd.W2Employer1,
            fd.W2Employer2, fd.OtherIncome, fd.CreatedDate);
    }
}
