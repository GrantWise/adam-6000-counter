namespace Industrial.Adam.Oee.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Equipment Scheduling service integration
/// </summary>
public sealed class EquipmentSchedulingSettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "EquipmentScheduling";

    /// <summary>
    /// Base URL for the Equipment Scheduling API
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// API version to use
    /// </summary>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>
    /// Timeout for HTTP requests
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay for exponential backoff retry policy
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Circuit breaker failure threshold
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker sampling duration
    /// </summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Circuit breaker minimum throughput
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Circuit breaker duration of break
    /// </summary>
    public TimeSpan CircuitBreakerDurationOfBreak { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable caching for availability queries
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Cache TTL for availability data
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache TTL for schedule data
    /// </summary>
    public TimeSpan ScheduleCacheTtl { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Health check endpoint path
    /// </summary>
    public string HealthCheckEndpoint { get; set; } = "/health";

    /// <summary>
    /// Health check timeout
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Enable fallback to default availability when service is unavailable
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Default availability percentage when service is unavailable (0.0 to 1.0)
    /// </summary>
    public decimal DefaultAvailability { get; set; } = 1.0m;

    /// <summary>
    /// Default operating hours per day when service is unavailable
    /// </summary>
    public decimal DefaultOperatingHours { get; set; } = 24m;

    /// <summary>
    /// Resource ID mapping for line ID to resource ID conversion
    /// Format: "lineId:resourceId;lineId2:resourceId2"
    /// </summary>
    public string ResourceMappings { get; set; } = string.Empty;

    /// <summary>
    /// Enable detailed logging for integration calls
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Log sensitive data (URLs, request/response bodies) - use only in development
    /// </summary>
    public bool LogSensitiveData { get; set; } = false;

    /// <summary>
    /// Gets the full base URL with API version
    /// </summary>
    public string GetApiBaseUrl() => $"{BaseUrl.TrimEnd('/')}/api/{ApiVersion}";

    /// <summary>
    /// Gets the parsed resource mappings dictionary
    /// </summary>
    public IReadOnlyDictionary<string, long> GetResourceMappings()
    {
        var mappings = new Dictionary<string, long>();

        if (string.IsNullOrWhiteSpace(ResourceMappings))
            return mappings.AsReadOnly();

        try
        {
            foreach (var mapping in ResourceMappings.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = mapping.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && long.TryParse(parts[1], out var resourceId))
                {
                    mappings[parts[0].Trim()] = resourceId;
                }
            }
        }
        catch
        {
            // Return empty dictionary if parsing fails
        }

        return mappings.AsReadOnly();
    }

    /// <summary>
    /// Maps a line ID to a resource ID
    /// </summary>
    /// <param name="lineId">Line identifier</param>
    /// <returns>Resource ID, or null if no mapping exists</returns>
    public long? MapLineIdToResourceId(string lineId)
    {
        var mappings = GetResourceMappings();
        return mappings.TryGetValue(lineId, out var resourceId) ? resourceId : null;
    }

    /// <summary>
    /// Validates the configuration settings
    /// </summary>
    /// <returns>Validation results</returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BaseUrl))
            errors.Add("BaseUrl is required");
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            errors.Add("BaseUrl must be a valid absolute URL");

        if (string.IsNullOrWhiteSpace(ApiVersion))
            errors.Add("ApiVersion is required");

        if (RequestTimeout <= TimeSpan.Zero)
            errors.Add("RequestTimeout must be greater than zero");

        if (MaxRetryAttempts < 0 || MaxRetryAttempts > 10)
            errors.Add("MaxRetryAttempts must be between 0 and 10");

        if (RetryBaseDelay <= TimeSpan.Zero)
            errors.Add("RetryBaseDelay must be greater than zero");

        if (CircuitBreakerFailureThreshold <= 0)
            errors.Add("CircuitBreakerFailureThreshold must be greater than zero");

        if (CircuitBreakerSamplingDuration <= TimeSpan.Zero)
            errors.Add("CircuitBreakerSamplingDuration must be greater than zero");

        if (CircuitBreakerMinimumThroughput <= 0)
            errors.Add("CircuitBreakerMinimumThroughput must be greater than zero");

        if (CircuitBreakerDurationOfBreak <= TimeSpan.Zero)
            errors.Add("CircuitBreakerDurationOfBreak must be greater than zero");

        if (CacheTtl <= TimeSpan.Zero)
            errors.Add("CacheTtl must be greater than zero");

        if (ScheduleCacheTtl <= TimeSpan.Zero)
            errors.Add("ScheduleCacheTtl must be greater than zero");

        if (DefaultAvailability < 0 || DefaultAvailability > 1)
            errors.Add("DefaultAvailability must be between 0.0 and 1.0");

        if (DefaultOperatingHours < 0 || DefaultOperatingHours > 24)
            errors.Add("DefaultOperatingHours must be between 0 and 24");

        if (HealthCheckTimeout <= TimeSpan.Zero)
            errors.Add("HealthCheckTimeout must be greater than zero");

        return errors;
    }
}
