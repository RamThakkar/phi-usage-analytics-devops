using System.Collections.Concurrent;

namespace PhiUsageAnalytics.Api.Services;

/// <summary>
/// In-memory session token management.
/// Stores active sessions with expiry. Resets on app restart (users re-login).
/// </summary>
public class SessionService
{
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
    private readonly ConcurrentDictionary<string, LoginAttemptInfo> _loginAttempts = new();

    private const int SessionExpiryHours = 8;
    private const int MaxLoginAttempts = 5;
    private const int LockoutMinutes = 15;

    /// <summary>
    /// Creates a new session token for the given organization.
    /// </summary>
    public string CreateSession(string organizationId, string organizationName)
    {
        var token = Guid.NewGuid().ToString("N");
        var session = new SessionInfo
        {
            Token = token,
            OrganizationId = organizationId,
            OrganizationName = organizationName,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(SessionExpiryHours)
        };

        _sessions[token] = session;

        // Clean up expired sessions periodically
        CleanupExpiredSessions();

        return token;
    }

    /// <summary>
    /// Validates a token and returns the session info if valid.
    /// </summary>
    public SessionInfo? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        if (_sessions.TryGetValue(token, out var session))
        {
            if (session.ExpiresAt > DateTime.UtcNow)
            {
                return session;
            }

            // Token expired — remove it
            _sessions.TryRemove(token, out _);
        }

        return null;
    }

    /// <summary>
    /// Removes a session (logout).
    /// </summary>
    public void RemoveSession(string token)
    {
        _sessions.TryRemove(token, out _);
    }

    /// <summary>
    /// Checks if an IP is locked out from login attempts.
    /// Returns true if locked out.
    /// </summary>
    public bool IsLockedOut(string ipAddress)
    {
        if (_loginAttempts.TryGetValue(ipAddress, out var info))
        {
            if (info.LockedUntil.HasValue && info.LockedUntil > DateTime.UtcNow)
            {
                return true;
            }

            // Lockout expired — reset
            if (info.LockedUntil.HasValue && info.LockedUntil <= DateTime.UtcNow)
            {
                _loginAttempts.TryRemove(ipAddress, out _);
            }
        }

        return false;
    }

    /// <summary>
    /// Records a failed login attempt. Returns true if now locked out.
    /// </summary>
    public bool RecordFailedAttempt(string ipAddress)
    {
        var info = _loginAttempts.GetOrAdd(ipAddress, _ => new LoginAttemptInfo());
        info.FailedAttempts++;
        info.LastAttempt = DateTime.UtcNow;

        if (info.FailedAttempts >= MaxLoginAttempts)
        {
            info.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears failed attempts on successful login.
    /// </summary>
    public void ClearFailedAttempts(string ipAddress)
    {
        _loginAttempts.TryRemove(ipAddress, out _);
    }

    private void CleanupExpiredSessions()
    {
        var expired = _sessions.Where(s => s.Value.ExpiresAt <= DateTime.UtcNow).Select(s => s.Key).ToList();
        foreach (var key in expired)
        {
            _sessions.TryRemove(key, out _);
        }
    }
}

public class SessionInfo
{
    public string Token { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class LoginAttemptInfo
{
    public int FailedAttempts { get; set; }
    public DateTime LastAttempt { get; set; }
    public DateTime? LockedUntil { get; set; }
}
