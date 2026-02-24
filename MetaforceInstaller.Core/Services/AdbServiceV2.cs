using MetaforceInstaller.Core.Interfaces;
using MetaforceInstaller.Core.Models;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.Core.Services;

public class AdbServiceV2 : IAdbService
{
    private readonly ILogger<AdbServiceV2> _logger;
    private readonly IAdbServerController _adbServerController;
    private readonly IDeviceProvider _deviceProvider;
    private readonly IAdbOperations _adbOperations;

    private readonly object _initLock = new();
    private Task? _serverStartTask;

    public AdbServiceV2(
        ILogger<AdbServiceV2> logger,
        IAdbServerController adbServerController,
        IDeviceProvider deviceProvider,
        IAdbOperations adbOperations)
    {
        _logger = logger;
        _adbServerController = adbServerController;
        _deviceProvider = deviceProvider;
        _adbOperations = adbOperations;
    }

    public Task InstallApkAsync(string apkPath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CopyFileAsync(string localPath, string remotePath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public DeviceInfo GetDeviceInfo()
    {
        throw new NotImplementedException();
    }

    public async Task PerformInstallAsync(string apkPath, string localPath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureServerStartedAsync(cancellationToken);

        var serial = _deviceProvider.SelectedDevice?.SerialNumber
                     ?? throw new InvalidOperationException("No device selected.");

        _logger.LogInformation("Starting full install to {Serial}", serial);

        await _adbOperations.InstallApkAsync(serial, apkPath, progress, cancellationToken);
        
        progress?.Report(new ProgressInfo
        {
            PercentageComplete = 0,
            Type = ProgressType.FileCopy,
            CurrentFile = Path.GetFileName(localPath),
            Message = "Подготовка к копированию файла..."
        });

        var remotePath = GetRemotePath(apkPath, localPath);
        await _adbOperations.PushFileAsync(serial, localPath, remotePath, progress, cancellationToken);
    }

    private Task EnsureServerStartedAsync(CancellationToken cancellationToken)
    {
        // cancellationToken тут используется только чтобы не начинать работу,
        // если уже попросили отмену до старта.
        cancellationToken.ThrowIfCancellationRequested();

        var task = _serverStartTask;
        if (task is not null)
            return task;

        lock (_initLock)
        {
            task = _serverStartTask;
            if (task is not null)
                return task;

            _serverStartTask = task = StartServerCoreAsync();
            return task;
        }
    }

    private async Task StartServerCoreAsync()
    {
        _logger.LogInformation("Starting ADB server...");
        await _adbServerController.StartAdbServerAsync();
        _logger.LogInformation("ADB server is ready.");
    }

    private static string GetRemotePath(string apkPath, string zipPath)
    {
        var apkInfo = ApkScrapper.GetApkInfo(apkPath);
        var zipName = Path.GetFileName(zipPath);
        return @$"/storage/emulated/0/Android/data/{apkInfo.PackageName}/files/{zipName}";
    }
}