using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Middleware;

/// <summary>
/// Middleware for adding security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _options = new SecurityHeadersOptions();
        configuration.GetSection("Security:Headers").Bind(_options);
    }

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        SecurityHeadersOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Adds security headers to the HTTP response
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Process the request first
        await _next(context);

        // Add security headers to the response
        AddSecurityHeaders(context);
    }

    /// <summary>
    /// Adds all configured security headers
    /// </summary>
    /// <param name="context">HTTP context</param>
    private void AddSecurityHeaders(HttpContext context)
    {
        var response = context.Response;
        var request = context.Request;

        try
        {
            // X-Frame-Options - Prevents clickjacking
            if (_options.XFrameOptions.Enabled && !response.Headers.ContainsKey("X-Frame-Options"))
            {
                response.Headers.Append("X-Frame-Options", _options.XFrameOptions.Value);
            }

            // X-Content-Type-Options - Prevents MIME type sniffing
            if (_options.XContentTypeOptions.Enabled && !response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                response.Headers.Append("X-Content-Type-Options", "nosniff");
            }

            // X-XSS-Protection - XSS filtering
            if (_options.XXssProtection.Enabled && !response.Headers.ContainsKey("X-XSS-Protection"))
            {
                response.Headers.Append("X-XSS-Protection", _options.XXssProtection.Value);
            }

            // Referrer-Policy - Controls referrer information
            if (_options.ReferrerPolicy.Enabled && !response.Headers.ContainsKey("Referrer-Policy"))
            {
                response.Headers.Append("Referrer-Policy", _options.ReferrerPolicy.Value);
            }

            // Permissions-Policy - Feature policy
            if (_options.PermissionsPolicy.Enabled && !response.Headers.ContainsKey("Permissions-Policy"))
            {
                response.Headers.Append("Permissions-Policy", _options.PermissionsPolicy.Value);
            }

            // Strict-Transport-Security - HTTPS enforcement
            if (_options.StrictTransportSecurity.Enabled &&
                request.IsHttps &&
                !response.Headers.ContainsKey("Strict-Transport-Security"))
            {
                response.Headers.Append("Strict-Transport-Security", _options.StrictTransportSecurity.Value);
            }

            // Content-Security-Policy - XSS and injection protection
            if (_options.ContentSecurityPolicy.Enabled && !response.Headers.ContainsKey("Content-Security-Policy"))
            {
                var csp = BuildContentSecurityPolicy(context);
                response.Headers.Append("Content-Security-Policy", csp);
            }

            // Additional custom headers
            foreach (var customHeader in _options.CustomHeaders)
            {
                if (!response.Headers.ContainsKey(customHeader.Key))
                {
                    response.Headers.Append(customHeader.Key, customHeader.Value);
                }
            }

            // Remove potentially dangerous headers
            RemoveDangerousHeaders(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding security headers");
        }
    }

    /// <summary>
    /// Builds Content Security Policy header value
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>CSP header value</returns>
    private string BuildContentSecurityPolicy(HttpContext context)
    {
        var csp = _options.ContentSecurityPolicy;
        var policies = new List<string>();

        // Generate nonce for this request if enabled
        string? nonce = null;
        if (csp.NonceGeneration)
        {
            nonce = GenerateNonce();
            context.Items["csp-nonce"] = nonce;
        }

        // Default source
        if (!string.IsNullOrEmpty(csp.DefaultSrc))
            policies.Add($"default-src {csp.DefaultSrc}");

        // Script sources with nonce if enabled
        if (!string.IsNullOrEmpty(csp.ScriptSrc))
        {
            var scriptSrc = csp.ScriptSrc;
            if (!string.IsNullOrEmpty(nonce))
                scriptSrc += $" 'nonce-{nonce}'";
            policies.Add($"script-src {scriptSrc}");
        }

        // Style sources with nonce if enabled
        if (!string.IsNullOrEmpty(csp.StyleSrc))
        {
            var styleSrc = csp.StyleSrc;
            if (!string.IsNullOrEmpty(nonce))
                styleSrc += $" 'nonce-{nonce}'";
            policies.Add($"style-src {styleSrc}");
        }

        // Image sources
        if (!string.IsNullOrEmpty(csp.ImgSrc))
            policies.Add($"img-src {csp.ImgSrc}");

        // Connect sources (AJAX, WebSocket, etc.)
        if (!string.IsNullOrEmpty(csp.ConnectSrc))
            policies.Add($"connect-src {csp.ConnectSrc}");

        // Font sources
        if (!string.IsNullOrEmpty(csp.FontSrc))
            policies.Add($"font-src {csp.FontSrc}");

        // Object sources (plugins)
        if (!string.IsNullOrEmpty(csp.ObjectSrc))
            policies.Add($"object-src {csp.ObjectSrc}");

        // Media sources
        if (!string.IsNullOrEmpty(csp.MediaSrc))
            policies.Add($"media-src {csp.MediaSrc}");

        // Frame sources
        if (!string.IsNullOrEmpty(csp.FrameSrc))
            policies.Add($"frame-src {csp.FrameSrc}");

        // Child sources (for web workers and nested frames)
        if (!string.IsNullOrEmpty(csp.ChildSrc))
            policies.Add($"child-src {csp.ChildSrc}");

        // Form action
        if (!string.IsNullOrEmpty(csp.FormAction))
            policies.Add($"form-action {csp.FormAction}");

        // Frame ancestors
        if (!string.IsNullOrEmpty(csp.FrameAncestors))
            policies.Add($"frame-ancestors {csp.FrameAncestors}");

        // Base URI
        if (!string.IsNullOrEmpty(csp.BaseUri))
            policies.Add($"base-uri {csp.BaseUri}");

        // Report URI for CSP violations
        if (!string.IsNullOrEmpty(csp.ReportUri))
            policies.Add($"report-uri {csp.ReportUri}");

        // Report-to for CSP violations (newer standard)
        if (!string.IsNullOrEmpty(csp.ReportTo))
            policies.Add($"report-to {csp.ReportTo}");

        // Upgrade insecure requests
        if (csp.UpgradeInsecureRequests)
            policies.Add("upgrade-insecure-requests");

        // Block all mixed content
        if (csp.BlockAllMixedContent)
            policies.Add("block-all-mixed-content");

        // Require Trusted Types
        if (csp.RequireTrustedTypes)
            policies.Add("require-trusted-types-for 'script'");

        return string.Join("; ", policies);
    }

    /// <summary>
    /// Generates a cryptographically secure nonce for CSP
    /// </summary>
    /// <returns>Base64-encoded nonce</returns>
    private static string GenerateNonce()
    {
        var bytes = new byte[16]; // 128-bit nonce
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Removes headers that might leak server information
    /// </summary>
    /// <param name="response">HTTP response</param>
    private static void RemoveDangerousHeaders(HttpResponse response)
    {
        var headersToRemove = new[]
        {
            "Server",           // Server version information
            "X-Powered-By",     // Framework information
            "X-AspNet-Version", // ASP.NET version
            "X-AspNetMvc-Version" // MVC version
        };

        foreach (var header in headersToRemove)
        {
            response.Headers.Remove(header);
        }
    }
}

/// <summary>
/// Configuration options for security headers
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>
    /// X-Frame-Options header configuration
    /// </summary>
    public HeaderOption XFrameOptions { get; set; } = new()
    {
        Enabled = true,
        Value = "DENY"
    };

    /// <summary>
    /// X-Content-Type-Options header configuration
    /// </summary>
    public HeaderOption XContentTypeOptions { get; set; } = new()
    {
        Enabled = true,
        Value = "nosniff"
    };

    /// <summary>
    /// X-XSS-Protection header configuration
    /// </summary>
    public HeaderOption XXssProtection { get; set; } = new()
    {
        Enabled = true,
        Value = "1; mode=block"
    };

    /// <summary>
    /// Referrer-Policy header configuration
    /// </summary>
    public HeaderOption ReferrerPolicy { get; set; } = new()
    {
        Enabled = true,
        Value = "strict-origin-when-cross-origin"
    };

    /// <summary>
    /// Permissions-Policy header configuration
    /// </summary>
    public HeaderOption PermissionsPolicy { get; set; } = new()
    {
        Enabled = true,
        Value = "camera=(), microphone=(), geolocation=(), payment=()"
    };

    /// <summary>
    /// Strict-Transport-Security header configuration
    /// </summary>
    public HeaderOption StrictTransportSecurity { get; set; } = new()
    {
        Enabled = true,
        Value = "max-age=31536000; includeSubDomains"
    };

    /// <summary>
    /// Content-Security-Policy configuration
    /// </summary>
    public ContentSecurityPolicyOptions ContentSecurityPolicy { get; set; } = new();

    /// <summary>
    /// Custom headers to add
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

/// <summary>
/// Configuration for individual security headers
/// </summary>
public class HeaderOption
{
    /// <summary>
    /// Whether this header is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Header value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Content Security Policy configuration options
/// </summary>
public class ContentSecurityPolicyOptions
{
    /// <summary>
    /// Whether CSP is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default source directive
    /// </summary>
    public string DefaultSrc { get; set; } = "'self'";

    /// <summary>
    /// Script source directive
    /// </summary>
    public string ScriptSrc { get; set; } = "'self'";

    /// <summary>
    /// Style source directive
    /// </summary>
    public string StyleSrc { get; set; } = "'self' 'unsafe-inline'";

    /// <summary>
    /// Image source directive
    /// </summary>
    public string ImgSrc { get; set; } = "'self' data:";

    /// <summary>
    /// Connect source directive (for AJAX, WebSocket, etc.)
    /// </summary>
    public string ConnectSrc { get; set; } = "'self'";

    /// <summary>
    /// Font source directive
    /// </summary>
    public string FontSrc { get; set; } = "'self'";

    /// <summary>
    /// Object source directive (for plugins)
    /// </summary>
    public string ObjectSrc { get; set; } = "'none'";

    /// <summary>
    /// Media source directive
    /// </summary>
    public string MediaSrc { get; set; } = "'self'";

    /// <summary>
    /// Frame source directive
    /// </summary>
    public string FrameSrc { get; set; } = "'none'";

    /// <summary>
    /// Child source directive
    /// </summary>
    public string ChildSrc { get; set; } = "'self'";

    /// <summary>
    /// Form action directive
    /// </summary>
    public string FormAction { get; set; } = "'self'";

    /// <summary>
    /// Frame ancestors directive
    /// </summary>
    public string FrameAncestors { get; set; } = "'none'";

    /// <summary>
    /// Base URI directive
    /// </summary>
    public string BaseUri { get; set; } = "'self'";

    /// <summary>
    /// Report URI for CSP violations
    /// </summary>
    public string ReportUri { get; set; } = string.Empty;

    /// <summary>
    /// Report-to for CSP violations (newer standard)
    /// </summary>
    public string ReportTo { get; set; } = string.Empty;

    /// <summary>
    /// Whether to upgrade insecure requests
    /// </summary>
    public bool UpgradeInsecureRequests { get; set; } = true;

    /// <summary>
    /// Whether to block all mixed content
    /// </summary>
    public bool BlockAllMixedContent { get; set; } = false;

    /// <summary>
    /// Whether to generate nonces for scripts and styles
    /// </summary>
    public bool NonceGeneration { get; set; } = false;

    /// <summary>
    /// Whether to require Trusted Types for scripts
    /// </summary>
    public bool RequireTrustedTypes { get; set; } = false;
}
