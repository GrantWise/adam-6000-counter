using System.Net;
using System.Net.Http.Json;
using Industrial.Adam.Oee.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Tests.Integration;

/// <summary>
/// Integration tests for OeeController
/// </summary>
public class OeeControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OeeControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetCurrentOee_WithValidDeviceId_ReturnsOeeMetrics()
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/oee/current?deviceId={deviceId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var metrics = await response.Content.ReadFromJsonAsync<OeeCalculationDto>();
            metrics.Should().NotBeNull();
            metrics!.ResourceReference.Should().Be(deviceId);
            metrics.OeePercentage.Should().BeInRange(0, 100);
            metrics.AvailabilityPercentage.Should().BeInRange(0, 100);
            metrics.PerformancePercentage.Should().BeInRange(0, 100);
            metrics.QualityPercentage.Should().BeInRange(0, 100);
        }
    }

    [Fact]
    public async Task GetCurrentOee_WithMissingDeviceId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/oee/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentOee_WithEmptyDeviceId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/oee/current?deviceId=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentOee_WithCustomTimeRange_ReturnsOeeMetrics()
    {
        // Arrange
        var deviceId = "TestDevice001";
        var startTime = DateTime.UtcNow.AddHours(-2).ToString("O");
        var endTime = DateTime.UtcNow.ToString("O");

        // Act
        var response = await _client.GetAsync($"/api/oee/current?deviceId={deviceId}&startTime={startTime}&endTime={endTime}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOeeHistory_WithValidDeviceId_ReturnsHistoricalData()
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/oee/history?deviceId={deviceId}&period=24");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await response.Content.ReadFromJsonAsync<IEnumerable<OeeCalculationDto>>();
        history.Should().NotBeNull();
        // History might be empty if no data exists, which is acceptable
    }

    [Fact]
    public async Task GetOeeHistory_WithInvalidPeriod_ReturnsBadRequest()
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/oee/history?deviceId={deviceId}&period=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOeeBreakdown_WithValidDeviceId_ReturnsDetailedMetrics()
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/oee/breakdown?deviceId={deviceId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var breakdown = await response.Content.ReadFromJsonAsync<OeeCalculationDto>();
            breakdown.Should().NotBeNull();
            breakdown!.ResourceReference.Should().Be(deviceId);

            // Breakdown should have the same structure as current OEE
            breakdown.OeePercentage.Should().BeInRange(0, 100);
            breakdown.AvailabilityPercentage.Should().BeInRange(0, 100);
            breakdown.PerformancePercentage.Should().BeInRange(0, 100);
            breakdown.QualityPercentage.Should().BeInRange(0, 100);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetOeeEndpoints_WithInvalidDeviceId_ReturnsBadRequest(string? deviceId)
    {
        // Arrange
        var encodedDeviceId = Uri.EscapeDataString(deviceId ?? "");

        // Act & Assert
        var currentResponse = await _client.GetAsync($"/api/oee/current?deviceId={encodedDeviceId}");
        currentResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var historyResponse = await _client.GetAsync($"/api/oee/history?deviceId={encodedDeviceId}");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var breakdownResponse = await _client.GetAsync($"/api/oee/breakdown?deviceId={encodedDeviceId}");
        breakdownResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OeeEndpoints_HealthCheck_ReturnsHealthy()
    {
        // This test ensures the application is properly configured for OEE endpoints

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
