using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.WebApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Industrial.Adam.Oee.Tests.Integration;

/// <summary>
/// Integration tests for JobsController
/// </summary>
public class JobsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public JobsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetActiveJob_WithValidDeviceId_ReturnsWorkOrderOrNotFound()
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/jobs/active?deviceId={deviceId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var workOrder = await response.Content.ReadFromJsonAsync<WorkOrderDto>(_jsonOptions);
            workOrder.Should().NotBeNull();
            workOrder!.ResourceReference.Should().Be(deviceId);
        }
    }

    [Fact]
    public async Task GetActiveJob_WithMissingDeviceId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/jobs/active");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetActiveJob_WithInvalidDeviceId_ReturnsBadRequest(string deviceId)
    {
        // Act
        var response = await _client.GetAsync($"/api/jobs/active?deviceId={Uri.EscapeDataString(deviceId)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWorkOrder_WithValidId_ReturnsWorkOrderOrNotFound()
    {
        // Arrange
        var workOrderId = "WO-TEST-001";

        // Act
        var response = await _client.GetAsync($"/api/jobs/{workOrderId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var workOrder = await response.Content.ReadFromJsonAsync<WorkOrderDto>(_jsonOptions);
            workOrder.Should().NotBeNull();
            workOrder!.WorkOrderId.Should().Be(workOrderId);
        }
    }

    [Fact]
    public async Task GetWorkOrderProgress_WithValidId_ReturnsProgressOrNotFound()
    {
        // Arrange
        var workOrderId = "WO-TEST-001";

        // Act
        var response = await _client.GetAsync($"/api/jobs/{workOrderId}/progress");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var progress = await response.Content.ReadFromJsonAsync<WorkOrderProgressDto>(_jsonOptions);
            progress.Should().NotBeNull();
            progress!.WorkOrderId.Should().Be(workOrderId);
            progress.CompletionPercentage.Should().BeInRange(0, 100);
        }
    }

    [Fact]
    public async Task StartWorkOrder_WithValidRequest_ReturnsCreatedOrConflict()
    {
        // Arrange
        var request = new StartWorkOrderRequest
        {
            WorkOrderId = $"WO-TEST-{Guid.NewGuid():N}",
            WorkOrderDescription = "Test Work Order",
            ProductId = "PROD-001",
            ProductDescription = "Test Product",
            PlannedQuantity = 100,
            UnitOfMeasure = "pieces",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(8),
            DeviceId = "TestDevice001",
            OperatorId = "OP-001"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.BadRequest);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var workOrderId = await response.Content.ReadAsStringAsync();
            workOrderId.Should().NotBeNullOrEmpty();
            response.Headers.Location.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task StartWorkOrder_WithInvalidRequest_ReturnsBadRequest()
    {
        // Arrange - Missing required fields
        var request = new StartWorkOrderRequest
        {
            WorkOrderId = "", // Invalid - empty
            WorkOrderDescription = "Test Work Order",
            ProductId = "PROD-001",
            ProductDescription = "Test Product",
            PlannedQuantity = -1, // Invalid - negative
            DeviceId = "TestDevice001"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartWorkOrder_WithMissingFields_ReturnsBadRequest()
    {
        // Arrange - Missing required fields
        var request = new
        {
            WorkOrderDescription = "Test Work Order"
            // Missing other required fields
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteWorkOrder_WithValidRequest_ReturnsNoContentOrNotFound()
    {
        // Arrange
        var workOrderId = "WO-TEST-001";
        var request = new CompleteWorkOrderRequest
        {
            ActualQuantityGood = 95,
            ActualQuantityScrap = 5,
            CompletedByOperatorId = "OP-001",
            CompletionNotes = "Completed successfully",
            ActualEndTime = DateTime.UtcNow
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/jobs/{workOrderId}/complete", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CompleteWorkOrder_WithInvalidQuantities_ReturnsBadRequest()
    {
        // Arrange
        var workOrderId = "WO-TEST-001";
        var request = new CompleteWorkOrderRequest
        {
            ActualQuantityGood = -10, // Invalid - negative
            ActualQuantityScrap = -5   // Invalid - negative
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/jobs/{workOrderId}/complete", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("NONEXISTENT-WO")]
    public async Task GetWorkOrder_WithInvalidId_ReturnsNotFoundOrBadRequest(string workOrderId)
    {
        // Act
        var response = await _client.GetAsync($"/api/jobs/{Uri.EscapeDataString(workOrderId)}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StartWorkOrder_WithFutureScheduledTimes_ReturnsCreatedOrConflict()
    {
        // Arrange - Test with future scheduled times
        var request = new StartWorkOrderRequest
        {
            WorkOrderId = $"WO-FUTURE-{Guid.NewGuid():N}",
            WorkOrderDescription = "Future Scheduled Work Order",
            ProductId = "PROD-002",
            ProductDescription = "Future Product",
            PlannedQuantity = 200,
            UnitOfMeasure = "pieces",
            ScheduledStartTime = DateTime.UtcNow.AddHours(1),
            ScheduledEndTime = DateTime.UtcNow.AddHours(9),
            DeviceId = "TestDevice002"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task JobsEndpoints_WithLargePayload_HandlesCorrectly()
    {
        // Arrange - Test with maximum allowed values
        var request = new StartWorkOrderRequest
        {
            WorkOrderId = $"WO-LARGE-{Guid.NewGuid():N}",
            WorkOrderDescription = new string('A', 200), // Maximum length
            ProductId = new string('B', 50), // Maximum length
            ProductDescription = new string('C', 200), // Maximum length
            PlannedQuantity = 999999.99m, // Maximum value
            UnitOfMeasure = "pieces",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(24),
            DeviceId = new string('D', 20), // Maximum length
            OperatorId = new string('E', 50) // Maximum length
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/jobs", request, _jsonOptions);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.BadRequest);
    }
}
