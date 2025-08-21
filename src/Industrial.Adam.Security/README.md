# Industrial Adam Security Library

## Phase 3 Security Infrastructure Implementation

This library provides comprehensive security logging, monitoring, input validation, and security headers for the Industrial Adam Counter System. It follows the established Logger module patterns for consistency and maintainability.

## Features Implemented

### ✅ Security Event Logging Framework
- Centralized `SecurityEventLogger` with structured logging
- Correlation ID tracking across all security events
- Support for authentication, authorization, validation, and suspicious activity logging
- Real-time security metrics collection and analysis

### ✅ Security Audit Middleware
- `SecurityAuditMiddleware` automatically captures security events
- Detects suspicious patterns (SQL injection, XSS, directory traversal)
- Logs authentication/authorization failures with context
- Monitors request performance and unusual access patterns

### ✅ Input Validation Framework
- Comprehensive validation attributes for preventing attacks:
  - `NoSqlInjectionAttribute` - Prevents SQL injection
  - `NoXssAttribute` - Prevents XSS attacks
  - `NoDirectoryTraversalAttribute` - Prevents directory traversal
  - `SafeFilePathAttribute` - Validates file paths
  - `EquipmentIdAttribute` - Industrial equipment ID validation
  - `WorkOrderNumberAttribute` - Work order format validation
- `InputValidationMiddleware` for request-level validation
- File upload security validation

### ✅ Security Headers and CSP
- `SecurityHeadersMiddleware` adds comprehensive security headers:
  - X-Frame-Options, X-Content-Type-Options, X-XSS-Protection
  - Referrer-Policy, Permissions-Policy
  - Strict-Transport-Security (HSTS)
  - Content-Security-Policy (CSP) with configurable directives
- Removes server information leakage headers

### ✅ Security Monitoring and Dashboard
- `SecurityMonitoringService` background service for threat detection
- Real-time monitoring of brute force and credential stuffing attacks
- Security health scoring and threat level assessment
- `SecurityMonitoringController` with monitoring endpoints:
  - `/api/security/status` - Current security status
  - `/api/security/metrics` - Security metrics for time periods
  - `/api/security/health` - Security health check
  - `/api/security/dashboard` - Comprehensive security dashboard
  - `/api/security/alerts` - Active security alerts

## Quick Start

### 1. Add Security Services to Program.cs

```csharp
using Industrial.Adam.Security.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add comprehensive security (includes authentication, logging, monitoring)
builder.Services.AddComprehensiveSecurity(builder.Configuration);

// Validate security configuration at startup
builder.Services.ValidateSecurityConfiguration(builder.Configuration);

var app = builder.Build();

// Use comprehensive security middleware pipeline
app.UseComprehensiveSecurityPipeline(builder.Configuration);

// Standard authentication/authorization
app.UseAuthentication();
app.UseAuthorization();

app.Run();
```

### 2. Configure Security Settings

Create or update your `appsettings.json`:

```json
{
  "Security": {
    "Headers": {
      "ContentSecurityPolicy": {
        "Enabled": true,
        "DefaultSrc": "'self'",
        "ScriptSrc": "'self' 'unsafe-inline'",
        "StyleSrc": "'self' 'unsafe-inline'"
      }
    },
    "Validation": {
      "MaxRequestSize": 10485760,
      "MaxParameterLength": 4096
    },
    "Monitoring": {
      "BruteForceThreshold": 10,
      "MinSuccessRate": 70.0
    }
  }
}
```

### 3. Set Environment Variables

```bash
# JWT Configuration
JWT_SECRET_KEY=your-secure-secret-key-here
JWT_ISSUER=Industrial.Adam.System
JWT_AUDIENCE=Industrial.Adam.APIs

# CORS Configuration
CORS_ORIGINS=https://yourdomain.com,https://admin.yourdomain.com
```

### 4. Use Validation Attributes

```csharp
public class CreateWorkOrderRequest
{
    [WorkOrderNumber]
    public string WorkOrderNumber { get; set; }

    [EquipmentId]
    public string EquipmentId { get; set; }

    [NoXss]
    [NoSqlInjection]
    public string Description { get; set; }
}
```

### 5. Manual Security Event Logging

```csharp
public class MyController : ControllerBase
{
    private readonly SecurityEventLogger _securityLogger;

    public MyController(SecurityEventLogger securityLogger)
    {
        _securityLogger = securityLogger;
    }

    [HttpPost]
    public IActionResult SomeAction([FromBody] MyRequest request)
    {
        // Log successful data access
        _securityLogger.LogSuspiciousActivity(
            "DataExport",
            $"User exported sensitive data: {request.DataType}",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            User.Identity?.Name,
            25);

        return Ok();
    }
}
```

## Security Monitoring

### Real-time Threat Detection

The security monitoring service automatically detects:

- **Brute Force Attacks**: Multiple failed authentication attempts from same IP
- **Credential Stuffing**: Multiple failed attempts across different usernames
- **Rapid Failures**: Quick succession of authentication failures
- **Suspicious Input Patterns**: SQL injection, XSS, directory traversal attempts
- **Abnormal Request Patterns**: Unusual file uploads, oversized requests

### Security Health Scoring

The system calculates a security health score (0-100) based on:
- Authentication success rate
- Number of suspicious activities
- High-risk security events
- Critical security incidents

### Monitoring Endpoints

- **GET /api/security/health** - Public health check endpoint
- **GET /api/security/status** - Detailed security status (Admin only)
- **GET /api/security/dashboard** - Complete security dashboard (Admin only)
- **GET /api/security/metrics** - Historical security metrics (Admin only)

## Configuration Options

### Security Headers

```json
{
  "Security": {
    "Headers": {
      "XFrameOptions": {
        "Enabled": true,
        "Value": "DENY"
      },
      "ContentSecurityPolicy": {
        "Enabled": true,
        "DefaultSrc": "'self'",
        "ScriptSrc": "'self' 'unsafe-inline'",
        "StyleSrc": "'self' 'unsafe-inline'",
        "ImgSrc": "'self' data: https:",
        "ConnectSrc": "'self'",
        "FrameSrc": "'none'",
        "ObjectSrc": "'none'"
      }
    }
  }
}
```

### Input Validation

```json
{
  "Security": {
    "Validation": {
      "MaxRequestSize": 10485760,
      "MaxParameterLength": 4096,
      "MaxHeaderLength": 8192,
      "MaxFileSize": 52428800,
      "AllowedFileExtensions": [".jpg", ".png", ".pdf", ".csv", ".json"]
    }
  }
}
```

### Monitoring Configuration

```json
{
  "Security": {
    "Monitoring": {
      "CheckInterval": "00:01:00",
      "BruteForceThreshold": 10,
      "CredentialStuffingThreshold": 15,
      "RapidFailureThreshold": 5,
      "MinSuccessRate": 70.0,
      "HighRiskEventThreshold": 5,
      "AlertWindowMinutes": 30
    }
  }
}
```

## Integration with Other Modules

### Logger Module Integration
This security library follows the same patterns as the Logger module for consistency:
- Structured service registration
- Configuration-based setup
- Comprehensive error handling
- Performance-optimized implementations

### OEE Module Integration
Use security logging in OEE operations:

```csharp
// Log work order security events
_securityLogger.LogConfigurationChange(
    "WorkOrderStatus",
    oldStatus,
    newStatus,
    username,
    ipAddress);
```

### Equipment Scheduling Integration
Validate scheduling requests:

```csharp
public class ScheduleRequest
{
    [EquipmentId]
    public string EquipmentId { get; set; }

    [NoXss]
    public string ScheduleNotes { get; set; }
}
```

## Architecture Alignment

This implementation follows Clean Architecture principles:

- **Domain Layer**: Security models and enums
- **Application Layer**: Security services and interfaces
- **Infrastructure Layer**: Middleware, controllers, and external integrations
- **Presentation Layer**: API endpoints and response models

## Production Considerations

### Performance
- Asynchronous processing of security events
- In-memory metrics caching with periodic cleanup
- Efficient pattern matching for threat detection
- Background processing for monitoring tasks

### Scalability
- Stateless middleware design
- Configurable monitoring intervals
- Queue-based event processing
- Memory-efficient data structures

### Security
- Correlation ID tracking for audit trails
- Sanitization of sensitive data in logs
- Rate limiting preparation (framework dependent)
- Defense-in-depth security headers

## Future Enhancements

- **Rate Limiting**: Full implementation when upgrading to .NET 7+
- **Persistent Storage**: Database storage for security events
- **Advanced Analytics**: Machine learning for threat detection
- **Integration APIs**: Webhook support for external SIEM systems
- **Compliance Reporting**: Automated security compliance reports

## Support

This library is part of the Industrial Adam Counter System and follows the established security remediation plan (Phase 3). All components are production-ready and follow industry best practices for industrial system security.