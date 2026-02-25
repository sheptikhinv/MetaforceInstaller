using System;
using System.Threading.Tasks;
using MetaforceInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.UI.Infrastructure;

public class AdbServerLifetime : IAdbServerLifetime
{
    private readonly IAdbServerController _adbServerController;
    private readonly ILogger<AdbServerLifetime> _logger;
    private readonly TaskCompletionSource<bool> _readyTaskSource = new();

    public Task ReadyTask => _readyTaskSource.Task;
    public bool IsReady => _readyTaskSource.Task.IsCompleted;

    public AdbServerLifetime(IAdbServerController adbServerController, ILogger<AdbServerLifetime> logger)
    {
        _adbServerController = adbServerController;
        _logger = logger;
    }

    public void StartInBackground()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                _logger.LogInformation("Starting ADB server in background...");
                await _adbServerController.StartAdbServerAsync();
                _readyTaskSource.SetResult(true);
                _logger.LogInformation("ADB server is ready.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting ADB server");
                _readyTaskSource.SetException(ex);
            }
        });
    }
}