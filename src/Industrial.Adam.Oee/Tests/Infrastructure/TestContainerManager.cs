using System.Collections.Concurrent;
using System.Data;
using Dapper;
using DotNet.Testcontainers.Builders;
using Industrial.Adam.Oee.Infrastructure;
using Industrial.Adam.Oee.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;

namespace Industrial.Adam.Oee.Tests.Infrastructure;

/// <summary>
/// Centralized test container management for OEE integration tests
/// Provides dynamic port allocation, health checks, and proper container lifecycle management
/// Based on Industrial.Adam.Logger test patterns with enhanced isolation
/// </summary>
public static class TestContainerManager
{
    private static readonly ConcurrentDictionary<string, PostgreSqlContainer> _containers = new();
    private static readonly ConcurrentDictionary<string, int> _portCounters = new();
    private static readonly object _lock = new();
    
    /// <summary>
    /// Base port for test containers - each test class gets a unique port range
    /// </summary>
    private const int BasePort = 55000;
    
    /// <summary>
    /// Creates a test container with a unique port for the calling test class
    /// Includes health checks and proper wait strategies
    /// </summary>
    public static PostgreSqlContainer CreateContainer(string testClassName)
    {
        lock (_lock)
        {
            var portOffset = GetNextPortOffset(testClassName);
            var uniquePort = BasePort + portOffset;
            
            var container = new PostgreSqlBuilder()
                .WithImage("timescale/timescaledb:latest-pg15")
                .WithDatabase("adam_counters")
                .WithUsername("adam_user")
                .WithPassword("adam_password")
                .WithPortBinding(uniquePort, 5432)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilPortIsAvailable(5432)
                    .UntilCommandIsCompleted("pg_isready", "-h", "localhost", "-p", "5432"))
                .WithStartupCallback(async (container, ct) =>
                {
                    // Verify container health after startup
                    await VerifyContainerHealthAsync(container);
                })
                .Build();
                
            _containers[testClassName] = container;
            return container;
        }
    }
    
    /// <summary>
    /// Gets or creates a container for the specified test class
    /// </summary>
    public static PostgreSqlContainer GetOrCreateContainer(string testClassName)
    {
        return _containers.GetOrAdd(testClassName, CreateContainer);
    }
    
    /// <summary>
    /// Properly disposes a container for the specified test class
    /// </summary>
    public static async Task DisposeContainerAsync(string testClassName)
    {
        if (_containers.TryRemove(testClassName, out var container))
        {
            try
            {
                await container.DisposeAsync();
            }
            catch (Exception)
            {
                // Ignore disposal exceptions during cleanup
            }
        }
    }
    
    /// <summary>
    /// Creates a connection factory for the test container
    /// </summary>
    public static IDbConnectionFactory CreateConnectionFactory(PostgreSqlContainer container, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<NpgsqlConnectionFactory>>();
        return new NpgsqlConnectionFactory(container.GetConnectionString(), logger);
    }
    
    /// <summary>
    /// Cleans up all containers - useful for test environment cleanup
    /// </summary>
    public static async Task DisposeAllAsync()
    {
        var disposalTasks = _containers.Values.Select(async container =>
        {
            try
            {
                await container.DisposeAsync();
            }
            catch (Exception)
            {
                // Ignore disposal exceptions during cleanup
            }
        });
        
        await Task.WhenAll(disposalTasks);
        _containers.Clear();
        _portCounters.Clear();
    }
    
    /// <summary>
    /// Sets up basic OEE database schema for testing
    /// </summary>
    public static async Task SetupOeeDatabaseAsync(IDbConnectionFactory connectionFactory)
    {
        using var connection = await connectionFactory.CreateConnectionAsync();
        
        // Enable TimescaleDB extension
        await connection.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS timescaledb CASCADE;");
        
        // Create counter_data table (from Industrial.Adam.Logger)
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS counter_data (
                timestamp TIMESTAMPTZ NOT NULL,
                device_id VARCHAR(20) NOT NULL,
                channel INTEGER NOT NULL,
                rate DECIMAL(10,2),
                processed_value DECIMAL(18,3),
                quality VARCHAR(10),
                PRIMARY KEY (timestamp, device_id, channel)
            );");
        
        // Convert to hypertable
        await connection.ExecuteAsync(@"
            SELECT create_hypertable('counter_data', 'timestamp', if_not_exists => TRUE);");
        
        // Create work_orders table (OEE-specific)
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS work_orders (
                work_order_id VARCHAR(50) PRIMARY KEY,
                work_order_description TEXT NOT NULL,
                product_id VARCHAR(50) NOT NULL,
                product_description TEXT NOT NULL,
                planned_quantity DECIMAL(18,3) NOT NULL,
                unit_of_measure VARCHAR(20) NOT NULL DEFAULT 'pieces',
                scheduled_start_time TIMESTAMPTZ NOT NULL,
                scheduled_end_time TIMESTAMPTZ NOT NULL,
                resource_reference VARCHAR(50) NOT NULL,
                status VARCHAR(20) NOT NULL DEFAULT 'Pending',
                actual_quantity_good DECIMAL(18,3) NOT NULL DEFAULT 0,
                actual_quantity_scrap DECIMAL(18,3) NOT NULL DEFAULT 0,
                actual_start_time TIMESTAMPTZ,
                actual_end_time TIMESTAMPTZ,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                
                CONSTRAINT work_orders_planned_quantity_positive CHECK (planned_quantity > 0),
                CONSTRAINT work_orders_actual_quantities_non_negative CHECK (
                    actual_quantity_good >= 0 AND actual_quantity_scrap >= 0
                ),
                CONSTRAINT work_orders_scheduled_times_valid CHECK (scheduled_end_time > scheduled_start_time),
                CONSTRAINT work_orders_status_valid CHECK (
                    status IN ('Pending', 'Active', 'Paused', 'Completed', 'Cancelled')
                )
            );");
        
        // Create performance indexes
        await connection.ExecuteAsync(@"
            CREATE INDEX IF NOT EXISTS idx_counter_data_device_timestamp_desc 
            ON counter_data(device_id, timestamp DESC)
            WHERE channel IN (0, 1);
            
            CREATE INDEX IF NOT EXISTS idx_work_orders_resource_status 
            ON work_orders(resource_reference, status) 
            WHERE status IN ('Active', 'Paused');");
    }
    
    /// <summary>
    /// Cleans test data from the database for test isolation
    /// </summary>
    public static async Task CleanupTestDataAsync(IDbConnectionFactory connectionFactory)
    {
        using var connection = await connectionFactory.CreateConnectionAsync();
        
        // Clean up test data
        await connection.ExecuteAsync("DELETE FROM work_orders WHERE work_order_id LIKE 'TEST_%' OR work_order_id LIKE 'WO-%' OR work_order_id LIKE 'PERF_%';");
        await connection.ExecuteAsync("DELETE FROM counter_data WHERE device_id LIKE 'TEST_%' OR device_id LIKE 'PERF_%';");
        
        // Reset sequences if any
        // Note: TimescaleDB hypertables don't use sequences, but this is good practice
    }
    
    /// <summary>
    /// Verifies container health after startup
    /// </summary>
    private static async Task VerifyContainerHealthAsync(PostgreSqlContainer container)
    {
        const int maxRetries = 10;
        const int delayMs = 1000;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var connection = new Npgsql.NpgsqlConnection(container.GetConnectionString());
                await connection.OpenAsync();
                
                var result = await connection.QuerySingleAsync<int>("SELECT 1");
                if (result == 1)
                {
                    return; // Health check passed
                }
            }
            catch (Exception)
            {
                if (i == maxRetries - 1)
                {
                    throw new InvalidOperationException($"Container health check failed after {maxRetries} attempts");
                }
                
                await Task.Delay(delayMs);
            }
        }
    }
    
    /// <summary>
    /// Gets the next available port offset for a test class
    /// Each test class gets a sequential port number to avoid conflicts
    /// </summary>
    private static int GetNextPortOffset(string testClassName)
    {
        return _portCounters.AddOrUpdate(testClassName, 
            key => _portCounters.Count, 
            (key, current) => current);
    }
}