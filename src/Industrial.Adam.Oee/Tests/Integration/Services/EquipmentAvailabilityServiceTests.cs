using System.Net;
using System.Text.Json;
using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.ValueObjects;
using Industrial.Adam.Oee.Infrastructure.Configuration;
using Industrial.Adam.Oee.Infrastructure.Services;
using Industrial.Adam.Oee.Tests.TestDoubles;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Integration.Services;

/// <summary>
/// Integration tests for Equipment Availability Service
/// Tests HTTP client integration, caching, and error handling scenarios
/// </summary>
public sealed class EquipmentAvailabilityServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<EquipmentAvailabilityService>> _loggerMock;
    private readonly EquipmentSchedulingSettings _settings;
    private readonly IEquipmentAvailabilityService _service;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public EquipmentAvailabilityServiceTests()
    {
        // Setup HTTP client mock
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000/api/v1/")
        };

        // Setup memory cache
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Setup logger mock
        _loggerMock = new Mock<ILogger<EquipmentAvailabilityService>>();

        // Setup settings
        _settings = new EquipmentSchedulingSettings
        {
            BaseUrl = "http://localhost:5000",
            ApiVersion = "v1",
            RequestTimeout = TimeSpan.FromSeconds(30),
            EnableCaching = true,
            CacheTtl = TimeSpan.FromMinutes(5),
            EnableFallback = true,
            DefaultAvailability = 0.8m,
            DefaultOperatingHours = 16m,
            ResourceMappings = "LINE001:1;LINE002:2;LINE003:3"
        };

        var optionsMock = new Mock<IOptions<EquipmentSchedulingSettings>>();
        optionsMock.Setup(x => x.Value).Returns(_settings);

        // Create service under test
        _service = new EquipmentAvailabilityService(_httpClient, _memoryCache, _loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task IsPlannedOperatingAsync_WithValidResponse_ReturnsCorrectStatus()
    {
        // Arrange
        var lineId = "LINE001";
        var timestamp = DateTime.UtcNow;
        var expectedResponse = new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                ResourceId = 1L,
                Timestamp = timestamp,
                IsOperating = true,
                ActiveSchedules = new object[0]
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _service.IsPlannedOperatingAsync(lineId, timestamp);

        // Assert
        Assert.True(result);
        VerifyHttpRequest("equipment-scheduling/availability/equipment/1/is-operating");
    }

    [Fact]
    public async Task IsPlannedOperatingAsync_WithHttpError_ReturnsFallbackStatus()
    {
        // Arrange
        var lineId = "LINE001";
        var timestamp = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc); // 10 AM - should be operating hours

        SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _service.IsPlannedOperatingAsync(lineId, timestamp);

        // Assert
        Assert.True(result); // Should be true for business hours (8 AM - 6 PM)
        VerifyLoggedWarning("Failed to get operating status");
    }

    [Fact]
    public async Task IsPlannedOperatingAsync_WithUnmappedLine_ReturnsFalse()
    {
        // Arrange
        var lineId = "UNMAPPED_LINE";
        var timestamp = DateTime.UtcNow;

        // Act
        var result = await _service.IsPlannedOperatingAsync(lineId, timestamp);

        // Assert
        Assert.False(result);
        VerifyLoggedWarning("No resource mapping found for line");
    }

    [Fact]
    public async Task GetPlannedHoursAsync_WithValidResponse_ReturnsMappedPlannedHours()
    {
        // Arrange
        var lineId = "LINE001";
        var date = DateTime.Today;
        var expectedSchedules = new List<EquipmentScheduleDto>
        {
            new()
            {
                Id = 1,
                ResourceId = 1,
                ScheduleDate = date,
                ShiftCode = "DAY",
                PlannedStartTime = date.AddHours(8),
                PlannedEndTime = date.AddHours(16),
                PlannedHours = 8m,
                ScheduleStatus = Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Active,
                IsException = false,
                GeneratedAt = DateTime.UtcNow
            }
        };

        var expectedResponse = new ApiResponse<DailyScheduleSummaryDto>
        {
            Success = true,
            Data = new DailyScheduleSummaryDto
            {
                Date = date,
                ResourceId = 1,
                ResourceName = "Production Line 1",
                TotalPlannedHours = 8m,
                ScheduleCount = 1,
                HasExceptions = false,
                Schedules = expectedSchedules
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _service.GetPlannedHoursAsync(lineId, date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(date, result.Date);
        Assert.Equal(8m, result.TotalHours);
        Assert.Single(result.Shifts);
        Assert.Equal("DAY", result.Shifts.First().ShiftCode);
        VerifyHttpRequest("equipment-scheduling/availability/equipment/1/daily-summary");
    }

    [Fact]
    public async Task GetPlannedHoursAsync_WithCachedData_ReturnsCachedResult()
    {
        // Arrange
        var lineId = "LINE001";
        var date = DateTime.Today;
        var cachedPlannedHours = new PlannedHours(date, 8m, new[]
        {
            new ScheduledShift("CACHED", new TimeOnly(8, 0), new TimeOnly(16, 0), 8m)
        });

        var cacheKey = $"planned_hours:{lineId}:{date:yyyy-MM-dd}";
        _memoryCache.Set(cacheKey, cachedPlannedHours);

        // Act
        var result = await _service.GetPlannedHoursAsync(lineId, date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(cachedPlannedHours.TotalHours, result.TotalHours);
        Assert.Equal("CACHED", result.Shifts.First().ShiftCode);

        // Verify no HTTP call was made
        _httpMessageHandlerMock.Protected()
            .Verify<Task<HttpResponseMessage>>("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetWeeklyScheduleAsync_WithValidResponse_ReturnsMappedSchedule()
    {
        // Arrange
        var lineId = "LINE002";
        var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1); // Monday
        var expectedSchedules = CreateWeeklySchedules(weekStart, 2);

        var expectedResponse = new ApiResponse<IEnumerable<EquipmentScheduleDto>>
        {
            Success = true,
            Data = expectedSchedules
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _service.GetWeeklyScheduleAsync(lineId, weekStart);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(lineId, result.LineId);
        Assert.Equal(weekStart, result.WeekStart);
        Assert.True(result.TotalWeeklyHours > 0);
        Assert.Equal(7, result.DailyPlannedHours.Count);
        VerifyHttpRequest("equipment-scheduling/availability/equipment/2/schedules");
    }

    [Fact]
    public async Task GetAvailabilitySummaryAsync_WithValidResponse_ReturnsMappedSummary()
    {
        // Arrange
        var lineId = "LINE003";
        var startDate = DateTime.Today.AddDays(-7);
        var endDate = DateTime.Today;
        var expectedSchedules = CreateWeeklySchedules(startDate, 3);

        var expectedResponse = new ApiResponse<ScheduleAvailabilityDto>
        {
            Success = true,
            Data = new ScheduleAvailabilityDto
            {
                ResourceId = 3,
                ResourceName = "Production Line 3",
                StartDate = startDate,
                EndDate = endDate,
                TotalPlannedHours = 56m,
                TotalAvailableHours = 168m,
                AvailabilityPercentage = 0.33m,
                ScheduledDays = 7,
                TotalDays = 8,
                Schedules = expectedSchedules.ToList()
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _service.GetAvailabilitySummaryAsync(lineId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(lineId, result.LineId);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(endDate, result.EndDate);
        Assert.Equal(56m, result.TotalPlannedHours);
        Assert.True(result.DailyBreakdown.Any());
        VerifyHttpRequest("equipment-scheduling/availability/equipment/3/availability");
    }

    [Fact]
    public async Task GetCurrentActiveSchedulesAsync_WithValidResponse_ReturnsMappedSchedules()
    {
        // Arrange
        var lineId = "LINE001";
        var currentTime = DateTime.UtcNow;
        var expectedSchedules = new List<EquipmentScheduleDto>
        {
            new()
            {
                Id = 1,
                ResourceId = 1,
                ScheduleDate = currentTime.Date,
                ShiftCode = "CURRENT",
                PlannedStartTime = currentTime.AddHours(-1),
                PlannedEndTime = currentTime.AddHours(1),
                PlannedHours = 2m,
                ScheduleStatus = Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Active,
                IsException = false,
                GeneratedAt = DateTime.UtcNow
            }
        };

        var expectedResponse = new ApiResponse<IEnumerable<EquipmentScheduleDto>>
        {
            Success = true,
            Data = expectedSchedules
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await _service.GetCurrentActiveSchedulesAsync(lineId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var activeSchedule = result.First();
        Assert.Equal(lineId, activeSchedule.LineId);
        Assert.Equal("CURRENT", activeSchedule.ShiftCode);
        Assert.Equal(Industrial.Adam.Oee.Domain.ValueObjects.ScheduleStatus.Active, activeSchedule.Status);
        VerifyHttpRequest("equipment-scheduling/availability/current-active");
    }

    [Fact]
    public async Task CheckServiceHealthAsync_WithHealthyService_ReturnsHealthyResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "OK");

        // Act
        var result = await _service.CheckServiceHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Equipment Scheduling", result.ServiceName);
        Assert.Equal(ServiceHealthStatus.Healthy, result.Status);
        Assert.True(result.ResponseTime > TimeSpan.Zero);
        VerifyHttpRequest("health");
    }

    [Fact]
    public async Task CheckServiceHealthAsync_WithUnhealthyService_ReturnsUnhealthyResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.ServiceUnavailable, "Service Unavailable");

        // Act
        var result = await _service.CheckServiceHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Equipment Scheduling", result.ServiceName);
        Assert.Equal(ServiceHealthStatus.Unhealthy, result.Status);
        Assert.Contains("503", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task CheckServiceHealthAsync_WithTimeout_ReturnsUnavailableResult()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        var result = await _service.CheckServiceHealthAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Equipment Scheduling", result.ServiceName);
        Assert.Equal(ServiceHealthStatus.Unavailable, result.Status);
        Assert.Contains("timeout", result.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPlannedHoursAsync_WithFallbackEnabled_ReturnsDefaultHours()
    {
        // Arrange
        var lineId = "LINE001";
        var date = DateTime.Today;

        SetupHttpResponse(HttpStatusCode.InternalServerError, "Server Error");

        // Act
        var result = await _service.GetPlannedHoursAsync(lineId, date);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(date, result.Date);
        Assert.Equal(16m, result.TotalHours); // Default from settings
        Assert.Single(result.Shifts);
        Assert.Equal(0.5m, result.Confidence); // Reduced confidence for fallback
    }

    [Theory]
    [InlineData(true, 5)] // Cache enabled, 5 minute TTL
    [InlineData(false, 0)] // Cache disabled
    public async Task CachingBehavior_RespectsSettings(bool enableCaching, int cacheTtlMinutes)
    {
        // Arrange
        _settings.EnableCaching = enableCaching;
        _settings.CacheTtl = TimeSpan.FromMinutes(cacheTtlMinutes);

        var lineId = "LINE001";
        var date = DateTime.Today;
        var response = CreateValidDailySummaryResponse(date, 1);

        SetupHttpResponse(HttpStatusCode.OK, response);

        // Act - First call
        var result1 = await _service.GetPlannedHoursAsync(lineId, date);

        // Act - Second call immediately after
        var result2 = await _service.GetPlannedHoursAsync(lineId, date);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(result1.TotalHours, result2.TotalHours);

        if (enableCaching)
        {
            // Should only have made one HTTP call due to caching
            VerifyHttpCallCount(1);
        }
        else
        {
            // Should have made two HTTP calls without caching
            VerifyHttpCallCount(2);
        }
    }

    // Helper methods

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T responseContent)
    {
        var jsonContent = JsonSerializer.Serialize(responseContent, JsonOptions);
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        var httpResponse = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, "text/plain")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);
    }

    private void VerifyHttpRequest(string expectedPath)
    {
        _httpMessageHandlerMock.Protected()
            .Verify<Task<HttpResponseMessage>>("SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains(expectedPath)),
                ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyHttpCallCount(int expectedCount)
    {
        _httpMessageHandlerMock.Protected()
            .Verify<Task<HttpResponseMessage>>("SendAsync", Times.Exactly(expectedCount), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    private void VerifyLoggedWarning(string expectedMessagePart)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private static List<EquipmentScheduleDto> CreateWeeklySchedules(DateTime weekStart, long resourceId)
    {
        var schedules = new List<EquipmentScheduleDto>();

        for (int i = 0; i < 7; i++)
        {
            var date = weekStart.AddDays(i);
            schedules.Add(new EquipmentScheduleDto
            {
                Id = i + 1,
                ResourceId = resourceId,
                ScheduleDate = date,
                ShiftCode = "DAY",
                PlannedStartTime = date.AddHours(8),
                PlannedEndTime = date.AddHours(16),
                PlannedHours = 8m,
                ScheduleStatus = Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Active,
                IsException = false,
                GeneratedAt = DateTime.UtcNow
            });
        }

        return schedules;
    }

    private static ApiResponse<DailyScheduleSummaryDto> CreateValidDailySummaryResponse(DateTime date, long resourceId)
    {
        return new ApiResponse<DailyScheduleSummaryDto>
        {
            Success = true,
            Data = new DailyScheduleSummaryDto
            {
                Date = date,
                ResourceId = resourceId,
                ResourceName = $"Production Line {resourceId}",
                TotalPlannedHours = 8m,
                ScheduleCount = 1,
                HasExceptions = false,
                Schedules = new List<EquipmentScheduleDto>
                {
                    new()
                    {
                        Id = 1,
                        ResourceId = resourceId,
                        ScheduleDate = date,
                        ShiftCode = "DAY",
                        PlannedStartTime = date.AddHours(8),
                        PlannedEndTime = date.AddHours(16),
                        PlannedHours = 8m,
                        ScheduleStatus = Industrial.Adam.EquipmentScheduling.Domain.Enums.ScheduleStatus.Active,
                        IsException = false,
                        GeneratedAt = DateTime.UtcNow
                    }
                }
            }
        };
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _memoryCache?.Dispose();
        _httpMessageHandlerMock?.Reset();
    }
}
