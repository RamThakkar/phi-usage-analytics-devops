namespace PhiUsageAnalytics.Api.Services;

/// <summary>
/// Simple file-based error logger.
/// Logs to: errors/{yyyy-MM-dd}/error.log
/// Format: [HH:mm:ss] | ENDPOINT | ERROR_TYPE | Short message
/// </summary>
public class ErrorLogger
{
    private readonly string _basePath;
    private readonly object _lock = new();

    public ErrorLogger(IWebHostEnvironment env)
    {
        _basePath = Path.Combine(env.ContentRootPath, "errors");
    }

    /// <summary>
    /// Logs an error with context. Short and developer-friendly.
    /// </summary>
    public void LogError(string endpoint, Exception ex, string? additionalContext = null)
    {
        try
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var folderPath = Path.Combine(_basePath, today);
            Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, "error.log");
            var time = DateTime.Now.ToString("HH:mm:ss");
            var errorType = ex.GetType().Name;
            var message = ex.Message.Length > 200 ? ex.Message.Substring(0, 200) + "..." : ex.Message;

            // Short format: [Time] | Endpoint | ErrorType | Message | StackTrace (first 2 lines)
            var stackLines = ex.StackTrace?.Split('\n').Take(2).Select(s => s.Trim()) ?? Array.Empty<string>();
            var shortStack = string.Join(" → ", stackLines);
            if (shortStack.Length > 300) shortStack = shortStack.Substring(0, 300) + "...";

            var logEntry = $"[{time}] | {endpoint} | {errorType} | {message}";
            if (!string.IsNullOrWhiteSpace(additionalContext))
                logEntry += $" | Context: {additionalContext}";
            logEntry += $"\n         Stack: {shortStack}\n";

            lock (_lock)
            {
                File.AppendAllText(filePath, logEntry + "\n");
            }
        }
        catch
        {
            // Logging should never crash the app
        }
    }
}
