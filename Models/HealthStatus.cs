namespace BackgroundService.Models;

/// <summary>
/// Health status of a background service.
/// </summary>
public class HealthStatus
{
    public DateTime LastRun { get; set; }
    public DateTime LastSuccess { get; set; }
    public string LastError { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
}