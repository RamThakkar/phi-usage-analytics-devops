namespace PhiUsageAnalytics.Api.Services;

/// <summary>
/// Logs visitor login/logout events to a single text file.
/// File: visits/visitor-log.txt (append mode, never deleted).
/// </summary>
public class VisitorLogger
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public VisitorLogger(IWebHostEnvironment env)
    {
        var folder = Path.Combine(env.ContentRootPath, "visits");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "visitor-log.txt");
    }

    public void LogLogin(string username, string organizationName, string ipAddress)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] | LOGIN  | {username} | {organizationName} | IP: {ipAddress}";
        Append(entry);
    }

    public void LogLogout(string username, string organizationName, string ipAddress)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] | LOGOUT | {username} | {organizationName} | IP: {ipAddress}";
        Append(entry);
    }

    public void LogFailedLogin(string username, string ipAddress)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] | FAILED | {username} | IP: {ipAddress}";
        Append(entry);
    }

    private void Append(string entry)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_filePath, entry + Environment.NewLine);
            }
        }
        catch { /* Never crash the app */ }
    }
}
