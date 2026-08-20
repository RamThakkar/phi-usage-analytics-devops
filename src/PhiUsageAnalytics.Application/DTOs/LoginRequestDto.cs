namespace PhiUsageAnalytics.Application.DTOs;

/// <summary>
/// Login request with simple username and password.
/// Mapped to OrganizationId via appsettings.
/// </summary>
public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response with org name and token/status.
/// </summary>
public class LoginResponseDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
}
