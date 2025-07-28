using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Logger.Core.Configuration;

/// <summary>
/// InfluxDB connection and write settings
/// </summary>
public class InfluxDbSettings
{
    /// <summary>
    /// InfluxDB server URL
    /// </summary>
    [Required(ErrorMessage = "InfluxDB URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string Url { get; set; } = "http://localhost:8086";
    
    /// <summary>
    /// Authentication token
    /// </summary>
    [Required(ErrorMessage = "InfluxDB token is required")]
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Organization name
    /// </summary>
    [Required(ErrorMessage = "Organization is required")]
    public string Organization { get; set; } = "adam_org";
    
    /// <summary>
    /// Bucket name for storing data
    /// </summary>
    [Required(ErrorMessage = "Bucket is required")]
    public string Bucket { get; set; } = "adam_counters";
    
    /// <summary>
    /// Batch size for writes
    /// </summary>
    [Range(1, 10000, ErrorMessage = "BatchSize must be between 1 and 10,000")]
    public int BatchSize { get; set; } = Constants.DefaultBatchSize;
    
    /// <summary>
    /// Batch timeout in milliseconds
    /// </summary>
    [Range(100, 60000, ErrorMessage = "BatchTimeoutMs must be between 100ms and 60 seconds")]
    public int BatchTimeoutMs { get; set; } = Constants.DefaultBatchTimeoutMs;
    
    /// <summary>
    /// Whether to use gzip compression
    /// </summary>
    public bool EnableGzip { get; set; } = true;
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    [Range(5, 300, ErrorMessage = "TimeoutSeconds must be between 5 and 300")]
    public int TimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Validate InfluxDB settings
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(Url))
        {
            errors.Add("InfluxDB URL cannot be empty");
        }
        else if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || 
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add($"Invalid InfluxDB URL: {Url}");
        }
        
        if (string.IsNullOrWhiteSpace(Token))
        {
            errors.Add("InfluxDB token cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(Organization))
        {
            errors.Add("InfluxDB organization cannot be empty");
        }
        
        if (string.IsNullOrWhiteSpace(Bucket))
        {
            errors.Add("InfluxDB bucket cannot be empty");
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}