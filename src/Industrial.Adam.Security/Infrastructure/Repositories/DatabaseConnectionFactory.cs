using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace Industrial.Adam.Security.Infrastructure.Repositories;

/// <summary>
/// Factory for creating database connections to TimescaleDB
/// </summary>
public class DatabaseConnectionFactory
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseConnectionFactory> _logger;

    public DatabaseConnectionFactory(IConfiguration configuration, ILogger<DatabaseConnectionFactory> logger)
    {
        _logger = logger;
        
        var host = configuration["TIMESCALEDB_HOST"] ?? "localhost";
        var port = configuration["TIMESCALEDB_PORT"] ?? "5433";
        var database = configuration["TIMESCALEDB_DATABASE"] ?? "adam_counters";
        var username = configuration["TIMESCALEDB_USERNAME"] ?? "industrial_system";
        var password = configuration["TIMESCALEDB_PASSWORD"] ?? throw new InvalidOperationException("TIMESCALEDB_PASSWORD must be configured");

        _connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";
        
        _logger.LogInformation("Database connection configured for {Host}:{Port}/{Database}", host, port, database);
    }

    /// <summary>
    /// Creates a new database connection
    /// </summary>
    /// <returns>Open database connection</returns>
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection");
            throw;
        }
    }

    /// <summary>
    /// Creates a new database connection synchronously
    /// </summary>
    /// <returns>Open database connection</returns>
    public IDbConnection CreateConnection()
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database connection");
            throw;
        }
    }
}