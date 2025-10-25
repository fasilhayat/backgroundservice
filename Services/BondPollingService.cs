namespace BackgroundService.Services;

using BackgroundService.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Polls the Tiwaz API for bond information every 5 minutes and writes health status to a file.
/// </summary>
public class BondPollingService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BondPollingService> _logger;

    // Cross-platform health file paths
    private readonly string _healthJsonFile = Path.Combine(Path.GetTempPath(), "healthstatus.json");
    private readonly string _healthTextFile = Path.Combine(Path.GetTempPath(), "healthy");

    private readonly HealthStatus _status = new();

    /// <summary>
    /// BondPollingService constructor.
    /// </summary>
    /// <param name="httpClientFactory">HttpClientFactory to create HTTP clients.</param>
    /// <param name="logger"></param>
    public BondPollingService(IHttpClientFactory httpClientFactory, ILogger<BondPollingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BondPollingService started at: {Time}", DateTimeOffset.Now);
        _logger.LogInformation("Health JSON file: {File}", _healthJsonFile);
        _logger.LogInformation("Health text file: {File}", _healthTextFile);

        while (!stoppingToken.IsCancellationRequested)
        {
            _status.LastRun = DateTime.UtcNow;

            try
            {
                await PollBondsApiAsync(stoppingToken);
                _status.LastSuccess = DateTime.UtcNow;
                _status.LastError = string.Empty;
            }
            catch (Exception ex)
            {
                _status.LastError = ex.Message;
                _logger.LogError(ex, "Error calling Tiwaz API");
            }

            await WriteHealthStatusAsync();

            _logger.LogInformation("Next check scheduled in 5 minutes...\n");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("BondPollingService stopping at: {Time}", DateTimeOffset.Now);
    }

    private async Task PollBondsApiAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("TiwazClient");

        _logger.LogInformation("Calling Tiwaz API at {Time}", DateTimeOffset.Now);

        var response = await client.GetAsync("v1/bonds", cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("=== Tiwaz API Response ===");
        Console.ResetColor();
        Console.WriteLine(content);
        Console.WriteLine("===========================\n");
    }

    private async Task WriteHealthStatusAsync()
    {
        try
        {
            // Compute IsHealthy (last success within 10 minutes)
            var isHealthy = (DateTime.UtcNow - _status.LastSuccess) < TimeSpan.FromMinutes(10);

            // Update status object with IsHealthy
            _status.IsHealthy = isHealthy;

            // Write JSON file
            var json = JsonSerializer.Serialize(_status, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_healthJsonFile, json);

            // Write plain-text health file for Kubernetes file probe
            await File.WriteAllTextAsync(_healthTextFile, isHealthy ? "healthy" : "unhealthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write health files.");
        }
    }
}
