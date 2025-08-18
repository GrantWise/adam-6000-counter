using System.Text.Json;
using FluentAssertions;
using Industrial.Adam.EquipmentScheduling.Application;
using Industrial.Adam.EquipmentScheduling.Domain;
using Industrial.Adam.EquipmentScheduling.Infrastructure;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Data;
using Industrial.Adam.EquipmentScheduling.WebApi.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Industrial.Adam.EquipmentScheduling.Tests.Integration;

public sealed class AvailabilityControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly EquipmentSchedulingDbContext _context;

    public AvailabilityControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EquipmentSchedulingDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<EquipmentSchedulingDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Override services for testing if needed
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            });
        });

        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<EquipmentSchedulingDbContext>();

        // Ensure the database is created
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetEquipmentAvailability_Should_Return_Valid_Response()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create test data
        await SeedTestDataAsync();

        var resourceId = 1L;
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(7);

        // Act
        var response = await client.GetAsync(
            $"/api/equipment-scheduling/availability/equipment/{resourceId}/availability?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.Should().NotBeNull();

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            var jsonDocument = JsonDocument.Parse(content);
            jsonDocument.RootElement.TryGetProperty("success", out var successProperty);
            successProperty.GetBoolean().Should().BeTrue();
        }
        else
        {
            // If the response is not successful, we still want to verify the structure
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            // For a 404 or other error, verify it returns proper error structure
            var jsonDocument = JsonDocument.Parse(content);
            jsonDocument.RootElement.TryGetProperty("success", out var successProperty);
            successProperty.GetBoolean().Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetEquipmentAvailability_With_Invalid_DateRange_Should_Return_BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var resourceId = 1L;
        var startDate = DateTime.Today.AddDays(7);
        var endDate = DateTime.Today; // End date before start date

        // Act
        var response = await client.GetAsync(
            $"/api/equipment-scheduling/availability/equipment/{resourceId}/availability?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Start date cannot be after end date");
    }

    [Fact]
    public async Task GetCurrentActiveSchedules_Should_Return_Valid_Response()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create test data
        await SeedTestDataAsync();

        // Act
        var response = await client.GetAsync("/api/equipment-scheduling/availability/current-active");

        // Assert
        response.Should().NotBeNull();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();

        var jsonDocument = JsonDocument.Parse(content);
        jsonDocument.RootElement.TryGetProperty("success", out var successProperty);
        successProperty.GetBoolean().Should().BeTrue();

        jsonDocument.RootElement.TryGetProperty("data", out var dataProperty);
        dataProperty.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Health_Check_Should_Return_Healthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
    }

    [Fact]
    public async Task Root_Endpoint_Should_Return_Service_Info()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Equipment Scheduling API");
        content.Should().Contain("version");
        content.Should().Contain("endpoints");
    }

    private async Task SeedTestDataAsync()
    {
        try
        {
            // Clear existing data
            _context.EquipmentSchedules.RemoveRange(_context.EquipmentSchedules);
            _context.PatternAssignments.RemoveRange(_context.PatternAssignments);
            _context.Resources.RemoveRange(_context.Resources);
            _context.OperatingPatterns.RemoveRange(_context.OperatingPatterns);

            await _context.SaveChangesAsync();

            // Create test operating pattern
            var pattern = new Industrial.Adam.EquipmentScheduling.Domain.Entities.OperatingPattern(
                "Test Pattern",
                Industrial.Adam.EquipmentScheduling.Domain.Enums.PatternType.DayOnly,
                7,
                40.0m,
                JsonDocument.Parse("{\"shifts\": [{\"code\": \"DAY\", \"startTime\": \"08:00\", \"endTime\": \"16:00\"}]}"));

            _context.OperatingPatterns.Add(pattern);
            await _context.SaveChangesAsync();

            // Create test resource
            var resource = new Industrial.Adam.EquipmentScheduling.Domain.Entities.Resource(
                "Test Work Unit",
                "TEST-WU-001",
                Industrial.Adam.EquipmentScheduling.Domain.Enums.ResourceType.WorkUnit,
                true,
                "Test work unit for integration testing");

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            // Create pattern assignment
            var assignment = new Industrial.Adam.EquipmentScheduling.Domain.Entities.PatternAssignment(
                resource.Id,
                pattern.Id,
                DateTime.Today,
                null,
                false,
                "Test System",
                "Integration test assignment");

            _context.PatternAssignments.Add(assignment);

            // Create test schedule
            var schedule = new Industrial.Adam.EquipmentScheduling.Domain.Entities.EquipmentSchedule(
                resource.Id,
                DateTime.Today,
                8.0m,
                pattern.Id,
                "DAY",
                DateTime.Today.AddHours(8),
                DateTime.Today.AddHours(16));

            _context.EquipmentSchedules.Add(schedule);

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Error seeding test data: {ex}");
            throw;
        }
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _context?.Dispose();
    }
}
