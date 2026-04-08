using Microsoft.EntityFrameworkCore;
using FAid.API.Data;
using FAid.API.DTOs;
using FAid.API.Services;
using FluentAssertions;
using Xunit;

namespace FAid.Tests.Services;

public class ApplicationServiceTests : IDisposable
{
    private readonly FAidDbContext _context;
    private readonly ApplicationService _service;

    public ApplicationServiceTests()
    {
        var options = new DbContextOptionsBuilder<FAidDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FAidDbContext(options);
        _service = new ApplicationService(_context);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateApplicationAndQueueEntry()
    {
        var dto = new CreateApplicationDto("Smith", 5, 2, DateTime.UtcNow, "FA-2024-001");
        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.FamilyName.Should().Be("Smith");
        result.ApplicationNumber.Should().Be("FA-2024-001");
        result.Status.Should().Be("Pending");

        var queueEntry = await _context.ReviewQueues
            .FirstOrDefaultAsync(q => q.ApplicationId == result.ApplicationId);
        queueEntry.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedResults()
    {
        for (int i = 1; i <= 5; i++)
        {
            await _service.CreateAsync(new CreateApplicationDto(
                $"Family{i}", i, 1, DateTime.UtcNow, $"FA-2024-{i:D3}"));
        }

        var page1 = await _service.GetAllAsync(1, 3);
        var page2 = await _service.GetAllAsync(2, 3);

        page1.Should().HaveCount(3);
        page2.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFields()
    {
        var created = await _service.CreateAsync(
            new CreateApplicationDto("Johnson", 3, 1, DateTime.UtcNow, "FA-2024-002"));

        var updated = await _service.UpdateAsync(created.ApplicationId,
            new UpdateApplicationDto("Johnson-Updated", null, null, "Processing"));

        updated.Should().NotBeNull();
        updated!.FamilyName.Should().Be("Johnson-Updated");
        updated.Status.Should().Be("Processing");
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalseForNonExistent()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
