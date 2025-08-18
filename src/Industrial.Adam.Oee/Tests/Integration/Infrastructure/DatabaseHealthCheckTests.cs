using System.Data;
using Industrial.Adam.Oee.Infrastructure;
using Industrial.Adam.Oee.Infrastructure.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for database health check functionality
/// </summary>
public class DatabaseHealthCheckTests
{
    /// <summary>
    /// Test health check with successful database connection
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithWorkingDatabase_ReturnsHealthy()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockMigrationService = new Mock<IDatabaseMigrationService>();
        var mockLogger = new Mock<ILogger<DatabaseHealthCheck>>();

        // Setup successful connection
        mockConnectionFactory
            .Setup(f => f.CreateConnectionAsync())
            .ReturnsAsync(mockConnection.Object);

        // Setup successful schema validation
        mockMigrationService
            .Setup(s => s.IsSchemaCurrent(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockMigrationService
            .Setup(s => s.ValidateSchemaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatabaseValidationResult(
                IsValid: true,
                Issues: Array.Empty<string>().ToList(),
                MissingTables: Array.Empty<string>().ToList(),
                MissingIndexes: Array.Empty<string>().ToList()
            ));

        var healthCheck = new DatabaseHealthCheck(
            mockConnectionFactory.Object,
            mockMigrationService.Object,
            mockLogger.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("Database is healthy", result.Description);

        // Verify data contains expected metrics
        Assert.True(result.Data.ContainsKey("connectivity_status"));
        Assert.True(result.Data.ContainsKey("schema_is_current"));
        Assert.True(result.Data.ContainsKey("total_check_duration_ms"));
    }

    /// <summary>
    /// Test health check with database connection failure
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithConnectionFailure_ReturnsUnhealthy()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockMigrationService = new Mock<IDatabaseMigrationService>();
        var mockLogger = new Mock<ILogger<DatabaseHealthCheck>>();

        // Setup connection failure
        mockConnectionFactory
            .Setup(f => f.CreateConnectionAsync())
            .ThrowsAsync(new InvalidOperationException("Connection failed"));

        var healthCheck = new DatabaseHealthCheck(
            mockConnectionFactory.Object,
            mockMigrationService.Object,
            mockLogger.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Database connectivity failed", result.Description);

        // Verify error data is captured
        Assert.True(result.Data.ContainsKey("connectivity_status"));
        Assert.Equal("failed", result.Data["connectivity_status"]);
        Assert.True(result.Data.ContainsKey("connectivity_error"));
    }

    /// <summary>
    /// Test health check with schema validation issues
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithSchemaIssues_ReturnsDegraded()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockMigrationService = new Mock<IDatabaseMigrationService>();
        var mockLogger = new Mock<ILogger<DatabaseHealthCheck>>();

        // Setup successful connection
        mockConnectionFactory
            .Setup(f => f.CreateConnectionAsync())
            .ReturnsAsync(mockConnection.Object);

        // Setup schema validation failure
        mockMigrationService
            .Setup(s => s.IsSchemaCurrent(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        mockMigrationService
            .Setup(s => s.ValidateSchemaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatabaseValidationResult(
                IsValid: false,
                Issues: new[] { "Missing table: work_orders" }.ToList(),
                MissingTables: new[] { "work_orders" }.ToList(),
                MissingIndexes: Array.Empty<string>().ToList()
            ));

        mockMigrationService
            .Setup(s => s.GetPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "001-create-work-orders-table.sql" });

        var healthCheck = new DatabaseHealthCheck(
            mockConnectionFactory.Object,
            mockMigrationService.Object,
            mockLogger.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Database schema issues detected", result.Description);

        // Verify schema issue data is captured
        Assert.True(result.Data.ContainsKey("schema_is_valid"));
        Assert.False((bool)result.Data["schema_is_valid"]);
        Assert.True(result.Data.ContainsKey("schema_issues"));
        Assert.True(result.Data.ContainsKey("pending_migrations"));
    }

    /// <summary>
    /// Test health check context includes performance metrics
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_IncludesPerformanceMetrics()
    {
        // Arrange
        var mockConnectionFactory = new Mock<IDbConnectionFactory>();
        var mockConnection = new Mock<IDbConnection>();
        var mockMigrationService = new Mock<IDatabaseMigrationService>();
        var mockLogger = new Mock<ILogger<DatabaseHealthCheck>>();

        // Setup successful flow
        mockConnectionFactory
            .Setup(f => f.CreateConnectionAsync())
            .ReturnsAsync(mockConnection.Object);

        mockMigrationService
            .Setup(s => s.IsSchemaCurrent(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockMigrationService
            .Setup(s => s.ValidateSchemaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DatabaseValidationResult(
                IsValid: true,
                Issues: Array.Empty<string>().ToList(),
                MissingTables: Array.Empty<string>().ToList(),
                MissingIndexes: Array.Empty<string>().ToList()
            ));

        var healthCheck = new DatabaseHealthCheck(
            mockConnectionFactory.Object,
            mockMigrationService.Object,
            mockLogger.Object);

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert - Performance metrics are included
        Assert.True(result.Data.ContainsKey("total_check_duration_ms"));
        Assert.True(result.Data.ContainsKey("connectivity_check_ms"));
        Assert.True(result.Data.ContainsKey("schema_check_ms"));

        // Verify all values are numeric and reasonable
        var totalDuration = (long)result.Data["total_check_duration_ms"];
        var connectivityDuration = (long)result.Data["connectivity_check_ms"];
        var schemaDuration = (long)result.Data["schema_check_ms"];

        Assert.True(totalDuration >= 0);
        Assert.True(connectivityDuration >= 0);
        Assert.True(schemaDuration >= 0);
        Assert.True(totalDuration >= connectivityDuration);
        Assert.True(totalDuration >= schemaDuration);
    }
}
