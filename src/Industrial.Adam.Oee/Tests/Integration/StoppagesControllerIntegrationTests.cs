using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Industrial.Adam.Oee.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Industrial.Adam.Oee.Tests.Integration;

/// <summary>
/// Integration tests for StoppagesController
/// </summary>
public class StoppagesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public StoppagesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetCurrentStoppage_WithValidDeviceId_ReturnsStoppageOrNoContent()
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/stoppages/current?deviceId={deviceId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var stoppage = await response.Content.ReadFromJsonAsync<StoppageInfoDto>(_jsonOptions);
            stoppage.Should().NotBeNull();
            stoppage!.DeviceId.Should().Be(deviceId);
            stoppage.IsActive.Should().BeTrue();
            stoppage.DurationMinutes.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task GetCurrentStoppage_WithMissingDeviceId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stoppages/current");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetCurrentStoppage_WithInvalidDeviceId_ReturnsBadRequest(string deviceId)
    {
        // Act
        var response = await _client.GetAsync($"/api/stoppages/current?deviceId={Uri.EscapeDataString(deviceId)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCurrentStoppage_WithCustomMinimumMinutes_ReturnsCorrectResponse()
    {
        // Arrange
        var deviceId = "TestDevice001";
        var minimumMinutes = 10;

        // Act
        var response = await _client.GetAsync($"/api/stoppages/current?deviceId={deviceId}&minimumMinutes={minimumMinutes}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var stoppage = await response.Content.ReadFromJsonAsync<StoppageInfoDto>(_jsonOptions);
            stoppage.Should().NotBeNull();
            stoppage!.DurationMinutes.Should().BeGreaterThanOrEqualTo(minimumMinutes);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public async Task GetCurrentStoppage_WithInvalidMinimumMinutes_ReturnsBadRequest(int minimumMinutes)
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/stoppages/current?deviceId={deviceId}&minimumMinutes={minimumMinutes}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetStoppageHistory_WithValidDeviceId_ReturnsHistoricalStoppages()
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/stoppages?deviceId={deviceId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stoppages = await response.Content.ReadFromJsonAsync<IEnumerable<StoppageInfoDto>>(_jsonOptions);
        stoppages.Should().NotBeNull();
        // Stoppages collection might be empty if no historical data exists, which is acceptable
    }

    [Fact]
    public async Task GetStoppageHistory_WithCustomPeriod_ReturnsCorrectResponse()
    {
        // Arrange
        var deviceId = "TestDevice001";
        var period = 48; // 48 hours

        // Act
        var response = await _client.GetAsync($"/api/stoppages?deviceId={deviceId}&period={period}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stoppages = await response.Content.ReadFromJsonAsync<IEnumerable<StoppageInfoDto>>(_jsonOptions);
        stoppages.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStoppageHistory_WithCustomTimeRange_ReturnsCorrectResponse()
    {
        // Arrange
        var deviceId = "TestDevice001";
        var startTime = DateTime.UtcNow.AddDays(-1).ToString("O");
        var endTime = DateTime.UtcNow.ToString("O");

        // Act
        var response = await _client.GetAsync($"/api/stoppages?deviceId={deviceId}&startTime={startTime}&endTime={endTime}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stoppages = await response.Content.ReadFromJsonAsync<IEnumerable<StoppageInfoDto>>(_jsonOptions);
        stoppages.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(8761)]
    public async Task GetStoppageHistory_WithInvalidPeriod_ReturnsBadRequest(int period)
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/stoppages?deviceId={deviceId}&period={period}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(61)]
    public async Task GetStoppageHistory_WithInvalidMinimumMinutes_ReturnsBadRequest(int minimumMinutes)
    {
        // Arrange
        var deviceId = "TestDevice001";

        // Act
        var response = await _client.GetAsync($"/api/stoppages?deviceId={deviceId}&minimumMinutes={minimumMinutes}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetStoppageHistory_WithMissingDeviceId_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stoppages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ClassifyStoppage_ReturnsNotImplemented()
    {
        // Arrange
        var stoppageId = "STOP-001";
        var classification = new { Reason = "Maintenance", Category = "Planned" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/stoppages/{stoppageId}/classify", classification, _jsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);

        var problemDetails = await response.Content.ReadAsStringAsync();
        problemDetails.Should().Contain("Not Implemented");
        problemDetails.Should().Contain("classification functionality is not yet implemented");
    }

    [Fact]
    public async Task GetStoppageHistory_WithLongPeriod_HandlesCorrectly()
    {
        // Arrange - Test with maximum allowed period
        var deviceId = "TestDevice001";
        var period = 8760; // 1 year

        // Act
        var response = await _client.GetAsync($"/api/stoppages?deviceId={deviceId}&period={period}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stoppages = await response.Content.ReadFromJsonAsync<IEnumerable<StoppageInfoDto>>(_jsonOptions);
        stoppages.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentStoppage_WithMaximumAllowedMinutes_ReturnsCorrectResponse()
    {
        // Arrange
        var deviceId = "TestDevice001";
        var minimumMinutes = 60; // Maximum allowed

        // Act
        var response = await _client.GetAsync($"/api/stoppages/current?deviceId={deviceId}&minimumMinutes={minimumMinutes}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StoppageEndpoints_WithSpecialCharactersInDeviceId_HandlesCorrectly()
    {
        // Arrange
        var deviceId = "Device-001_Test@Location";
        var encodedDeviceId = Uri.EscapeDataString(deviceId);

        // Act
        var currentResponse = await _client.GetAsync($"/api/stoppages/current?deviceId={encodedDeviceId}");
        var historyResponse = await _client.GetAsync($"/api/stoppages?deviceId={encodedDeviceId}");

        // Assert
        currentResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
        historyResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }
}
