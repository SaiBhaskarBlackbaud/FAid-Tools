using Microsoft.EntityFrameworkCore;
using FAid.API.Data;
using FAid.API.DTOs;
using FAid.API.Services;
using FluentAssertions;
using Xunit;

namespace FAid.Tests.Services;

public class QueueServiceTests : IDisposable
{
    private readonly FAidDbContext _context;
    private readonly QueueService _service;

    public QueueServiceTests()
    {
        var options = new DbContextOptionsBuilder<FAidDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FAidDbContext(options);
        _service = new QueueService(_context);
    }

    [Fact]
    public async Task GetMetricsAsync_ShouldReturnCorrectCounts()
    {
        var appService = new ApplicationService(_context);
        await appService.CreateAsync(new CreateApplicationDto("A", 1, 1, DateTime.UtcNow, "FA-001"));
        await appService.CreateAsync(new CreateApplicationDto("B", 2, 2, DateTime.UtcNow, "FA-002"));

        var metrics = await _service.GetMetricsAsync();

        metrics.TotalApplications.Should().Be(2);
        metrics.PendingReviews.Should().Be(2);
        metrics.InQueueCount.Should().Be(2);
    }

    [Fact]
    public async Task GetQueueAsync_ShouldReturnAllItems()
    {
        var appService = new ApplicationService(_context);
        await appService.CreateAsync(new CreateApplicationDto("C", 3, 1, DateTime.UtcNow, "FA-003"));

        var queue = await _service.GetQueueAsync();
        queue.Should().HaveCount(1);
    }

    public void Dispose() => _context.Dispose();
}
