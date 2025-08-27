using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Industrial.Adam.Security.Infrastructure.Authorization;

/// <summary>
/// Resource-based authorization handler for fine-grained access control
/// </summary>
public class ResourceBasedAuthorizationHandler : AuthorizationHandler<ResourcePermissionRequirement, ResourceContext>
{
    private readonly ILogger<ResourceBasedAuthorizationHandler> _logger;

    public ResourceBasedAuthorizationHandler(ILogger<ResourceBasedAuthorizationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourcePermissionRequirement requirement,
        ResourceContext resource)
    {
        try
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("User ID or role not found in claims for resource access");
                context.Fail();
                return Task.CompletedTask;
            }

            _logger.LogDebug("Evaluating resource access for user {UserId} with role {UserRole} on resource {ResourceType}:{ResourceId}",
                userId, userRole, resource.ResourceType, resource.ResourceId);

            // Check basic role requirements
            if (!HasRequiredRole(userRole, requirement.RequiredRole))
            {
                _logger.LogWarning("User {UserId} with role {UserRole} does not have required role {RequiredRole}",
                    userId, userRole, requirement.RequiredRole);
                context.Fail();
                return Task.CompletedTask;
            }

            // Check resource-specific permissions
            if (!HasResourcePermission(context.User, resource, requirement.Permission))
            {
                _logger.LogWarning("User {UserId} does not have permission {Permission} on resource {ResourceType}:{ResourceId}",
                    userId, requirement.Permission, resource.ResourceType, resource.ResourceId);
                context.Fail();
                return Task.CompletedTask;
            }

            // Check IP restrictions if configured
            if (!CheckIpRestrictions(context, resource))
            {
                _logger.LogWarning("User {UserId} access denied due to IP restrictions", userId);
                context.Fail();
                return Task.CompletedTask;
            }

            // Check time-based restrictions
            if (!CheckTimeRestrictions(context, resource))
            {
                _logger.LogWarning("User {UserId} access denied due to time restrictions", userId);
                context.Fail();
                return Task.CompletedTask;
            }

            _logger.LogInformation("Access granted to user {UserId} for resource {ResourceType}:{ResourceId}",
                userId, resource.ResourceType, resource.ResourceId);

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource authorization");
            context.Fail();
            return Task.CompletedTask;
        }
    }

    private static bool HasRequiredRole(string userRole, string requiredRole)
    {
        // Define role hierarchy
        var roleHierarchy = new Dictionary<string, int>
        {
            ["User"] = 1,
            ["Operator"] = 2,
            ["Supervisor"] = 3,
            ["Admin"] = 4,
            ["SystemAdmin"] = 5
        };

        if (!roleHierarchy.TryGetValue(userRole, out var userRoleLevel) ||
            !roleHierarchy.TryGetValue(requiredRole, out var requiredRoleLevel))
        {
            return false;
        }

        return userRoleLevel >= requiredRoleLevel;
    }

    private bool HasResourcePermission(ClaimsPrincipal user, ResourceContext resource, string permission)
    {
        // Check user-specific permissions
        var userPermissions = user.FindAll("permission").Select(c => c.Value);
        
        // Build permission string (e.g., "users:read", "configurations:write")
        var resourcePermission = $"{resource.ResourceType.ToLowerInvariant()}:{permission.ToLowerInvariant()}";
        
        if (userPermissions.Contains(resourcePermission) || userPermissions.Contains("*:*"))
        {
            return true;
        }

        // Check resource ownership
        if (permission.ToLowerInvariant() == "read" || permission.ToLowerInvariant() == "write")
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (resource.OwnerId == userId)
            {
                return true;
            }
        }

        return false;
    }

    private bool CheckIpRestrictions(AuthorizationHandlerContext context, ResourceContext resource)
    {
        // Get IP address from context (this would come from HttpContext in a real implementation)
        var ipAddress = GetUserIpAddress(context);
        
        if (string.IsNullOrEmpty(ipAddress))
        {
            return true; // Allow if IP cannot be determined (could be made configurable)
        }

        // Check resource-specific IP restrictions
        if (resource.IpRestrictions != null && resource.IpRestrictions.Any())
        {
            return resource.IpRestrictions.Any(range => IsIpInRange(ipAddress, range));
        }

        // Check user-specific IP restrictions from claims
        var userIpRestrictions = context.User.FindAll("ip_whitelist").Select(c => c.Value);
        if (userIpRestrictions.Any())
        {
            return userIpRestrictions.Any(range => IsIpInRange(ipAddress, range));
        }

        return true; // No restrictions
    }

    private bool CheckTimeRestrictions(AuthorizationHandlerContext context, ResourceContext resource)
    {
        var currentTime = DateTime.UtcNow;
        
        // Check resource-specific time restrictions
        if (resource.AccessWindowStart.HasValue && resource.AccessWindowEnd.HasValue)
        {
            var start = resource.AccessWindowStart.Value;
            var end = resource.AccessWindowEnd.Value;
            
            if (currentTime < start || currentTime > end)
            {
                return false;
            }
        }

        // Check user-specific time restrictions from claims
        var userTimeRestriction = context.User.FindFirst("access_hours")?.Value;
        if (!string.IsNullOrEmpty(userTimeRestriction))
        {
            return IsWithinAllowedHours(currentTime, userTimeRestriction);
        }

        return true; // No restrictions
    }

    private static string? GetUserIpAddress(AuthorizationHandlerContext context)
    {
        // In a real implementation, this would extract IP from HttpContext
        // For now, return null as we don't have access to HttpContext here
        return context.User.FindFirst("ip_address")?.Value;
    }

    private static bool IsIpInRange(string ipAddress, string range)
    {
        // Simplified IP range checking - should use proper CIDR logic
        if (range == "*" || range == "0.0.0.0/0")
            return true;
            
        if (range.Contains('/'))
        {
            // CIDR notation - simplified check
            var parts = range.Split('/');
            if (parts.Length == 2 && ipAddress.StartsWith(parts[0].Split('.')[0]))
                return true;
        }
        else
        {
            // Exact match
            return ipAddress == range;
        }

        return false;
    }

    private static bool IsWithinAllowedHours(DateTime currentTime, string allowedHours)
    {
        // Parse allowed hours format "09:00-17:00"
        var parts = allowedHours.Split('-');
        if (parts.Length != 2)
            return true; // Invalid format, allow access

        if (TimeOnly.TryParse(parts[0], out var startTime) && 
            TimeOnly.TryParse(parts[1], out var endTime))
        {
            var currentTimeOnly = TimeOnly.FromDateTime(currentTime);
            return currentTimeOnly >= startTime && currentTimeOnly <= endTime;
        }

        return true; // Invalid format, allow access
    }
}

/// <summary>
/// Resource permission requirement for authorization
/// </summary>
public class ResourcePermissionRequirement : IAuthorizationRequirement
{
    public string RequiredRole { get; }
    public string Permission { get; }

    public ResourcePermissionRequirement(string requiredRole, string permission)
    {
        RequiredRole = requiredRole ?? throw new ArgumentNullException(nameof(requiredRole));
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}

/// <summary>
/// Resource context for authorization decisions
/// </summary>
public class ResourceContext
{
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string? OwnerId { get; set; }
    public List<string>? IpRestrictions { get; set; }
    public DateTime? AccessWindowStart { get; set; }
    public DateTime? AccessWindowEnd { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}