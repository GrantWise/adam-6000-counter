namespace Industrial.Adam.Security.Authorization;

/// <summary>
/// Constants for user roles in the Industrial Adam system
/// </summary>
public static class RoleConstants
{
    /// <summary>
    /// System administrator with full access to all functions
    /// </summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>
    /// Plant administrator with access to configuration and user management
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Production supervisor with access to OEE, scheduling, and monitoring
    /// </summary>
    public const string Supervisor = "Supervisor";

    /// <summary>
    /// Machine operator with read access to production data and basic controls
    /// </summary>
    public const string Operator = "Operator";

    /// <summary>
    /// Read-only viewer with access to dashboards and reports
    /// </summary>
    public const string Viewer = "Viewer";

    /// <summary>
    /// All available roles
    /// </summary>
    public static readonly string[] AllRoles = [
        SystemAdmin,
        Admin,
        Supervisor,
        Operator,
        Viewer
    ];

    /// <summary>
    /// Administrative roles (can manage users and system settings)
    /// </summary>
    public static readonly string[] AdminRoles = [
        SystemAdmin,
        Admin
    ];

    /// <summary>
    /// Production management roles (can control production processes)
    /// </summary>
    public static readonly string[] ProductionRoles = [
        SystemAdmin,
        Admin,
        Supervisor
    ];

    /// <summary>
    /// Operational roles (can view and interact with production data)
    /// </summary>
    public static readonly string[] OperationalRoles = [
        SystemAdmin,
        Admin,
        Supervisor,
        Operator
    ];

    /// <summary>
    /// All authenticated roles (any logged-in user)
    /// </summary>
    public static readonly string[] AuthenticatedRoles = AllRoles;

    /// <summary>
    /// Gets role hierarchy level (lower number = higher privilege)
    /// </summary>
    /// <param name="role">Role name</param>
    /// <returns>Hierarchy level (0-4)</returns>
    public static int GetRoleHierarchyLevel(string role)
    {
        return role switch
        {
            SystemAdmin => 0,
            Admin => 1,
            Supervisor => 2,
            Operator => 3,
            Viewer => 4,
            _ => int.MaxValue
        };
    }

    /// <summary>
    /// Checks if a role has higher or equal privilege than another role
    /// </summary>
    /// <param name="userRole">User's role</param>
    /// <param name="requiredRole">Required minimum role</param>
    /// <returns>True if user role has sufficient privilege</returns>
    public static bool HasSufficientPrivilege(string userRole, string requiredRole)
    {
        return GetRoleHierarchyLevel(userRole) <= GetRoleHierarchyLevel(requiredRole);
    }

    /// <summary>
    /// Gets role display name for UI
    /// </summary>
    /// <param name="role">Role name</param>
    /// <returns>Display name</returns>
    public static string GetRoleDisplayName(string role)
    {
        return role switch
        {
            SystemAdmin => "System Administrator",
            Admin => "Administrator",
            Supervisor => "Supervisor",
            Operator => "Operator",
            Viewer => "Viewer",
            _ => role
        };
    }

    /// <summary>
    /// Gets role description
    /// </summary>
    /// <param name="role">Role name</param>
    /// <returns>Role description</returns>
    public static string GetRoleDescription(string role)
    {
        return role switch
        {
            SystemAdmin => "Full system access including user management and system configuration",
            Admin => "Administrative access to plant operations and user management",
            Supervisor => "Production oversight with OEE monitoring and scheduling control",
            Operator => "Machine operation with production data access and basic controls",
            Viewer => "Read-only access to dashboards and production reports",
            _ => "Unknown role"
        };
    }
}
