namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// User domain entity with industrial security requirements
/// </summary>
public class User
{
    public string Id { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime? LastPasswordChangeAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? LockedUntil { get; private set; }
    public bool RequiresPasswordChange { get; private set; }
    public List<string> Permissions { get; private set; } = new();
    public string? IpWhitelistPattern { get; private set; }
    public bool MultiFactorEnabled { get; private set; }
    public string? MfaSecret { get; private set; }

    private User() { } // EF constructor

    public User(string id, string username, string email, string passwordHash, UserRole role)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        Role = role;
        Status = UserStatus.Active;
        CreatedAt = DateTime.UtcNow;
        RequiresPasswordChange = false;
        FailedLoginAttempts = 0;
        Permissions = new List<string>();
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public void RecordFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
        {
            Status = UserStatus.Locked;
            LockedUntil = DateTime.UtcNow.Add(lockoutDuration);
        }
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash ?? throw new ArgumentNullException(nameof(newPasswordHash));
        LastPasswordChangeAt = DateTime.UtcNow;
        RequiresPasswordChange = false;
        FailedLoginAttempts = 0;
    }

    public void UpdateRole(UserRole newRole)
    {
        Role = newRole;
    }

    public void UpdateStatus(UserStatus newStatus)
    {
        Status = newStatus;
        if (newStatus == UserStatus.Active)
        {
            FailedLoginAttempts = 0;
            LockedUntil = null;
        }
    }

    public void ForcePasswordChange()
    {
        RequiresPasswordChange = true;
    }

    public void AddPermission(string permission)
    {
        if (!string.IsNullOrEmpty(permission) && !Permissions.Contains(permission))
        {
            Permissions.Add(permission);
        }
    }

    public void RemovePermission(string permission)
    {
        Permissions.Remove(permission);
    }

    public void SetIpWhitelist(string? pattern)
    {
        IpWhitelistPattern = pattern;
    }

    public void EnableMultiFactor(string secret)
    {
        MfaSecret = secret ?? throw new ArgumentNullException(nameof(secret));
        MultiFactorEnabled = true;
    }

    public void DisableMultiFactor()
    {
        MultiFactorEnabled = false;
        MfaSecret = null;
    }

    public bool IsLocked => Status == UserStatus.Locked || (LockedUntil.HasValue && LockedUntil > DateTime.UtcNow);
    public bool IsActive => Status == UserStatus.Active && !IsLocked;
}

public enum UserRole
{
    User = 1,
    Operator = 2,
    Supervisor = 3,
    Admin = 4,
    SystemAdmin = 5
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3,
    Suspended = 4
}