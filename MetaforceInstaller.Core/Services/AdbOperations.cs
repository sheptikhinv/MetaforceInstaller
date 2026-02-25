using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using MetaforceInstaller.Core.Interfaces;
using MetaforceInstaller.Core.Models;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.Core.Services;

public class AdbOperations : IAdbOperations
{
    private readonly ILogger<AdbOperations> _logger;
    private readonly AdbClient _adbClient;
    private readonly IAdbServerLifetime _adbServerLifetime;

    public AdbOperations(ILogger<AdbOperations> logger, AdbClient adbClient, IAdbServerLifetime adbServerLifetime)
    {
        _logger = logger;
        _adbClient = adbClient;
        _adbServerLifetime = adbServerLifetime;
    }

    public async Task InstallApkAsync(
        string serial,
        string apkPath,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(apkPath))
            throw new FileNotFoundException("Could not find APK file.", apkPath);

        await _adbServerLifetime.ReadyTask;

        var deviceData = ResolveDevice(serial);
        if (deviceData is null)
            throw new InvalidOperationException($"Could not find device with serial: {serial}");

        _logger.LogInformation("Installing APK: {ApkPath}", apkPath);

        progress?.Report(new ProgressInfo
        {
            PercentageComplete = 0,
            Type = ProgressType.Installation,
            CurrentFile = Path.GetFileName(apkPath),
            Message = "Подготовка к установке APK..."
        });

        var packageManager = new PackageManager(_adbClient, deviceData.Value);

        await packageManager.InstallPackageAsync(
            apkPath,
            installProgress =>
            {
                var pct = (int)installProgress.UploadProgress;
                progress?.Report(new ProgressInfo
                {
                    PercentageComplete = pct,
                    Type = ProgressType.Installation,
                    CurrentFile = Path.GetFileName(apkPath),
                    Message = $"Установка APK: {installProgress.UploadProgress:F1}%"
                });
            },
            cancellationToken: cancellationToken);

        progress?.Report(new ProgressInfo
        {
            PercentageComplete = 100,
            Type = ProgressType.Installation,
            CurrentFile = Path.GetFileName(apkPath),
            Message = "Установка APK завершена"
        });

        _logger.LogInformation("APK installed successfully");
    }

    public async Task PushFileAsync(
        string serial,
        string localPath,
        string remotePath,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(localPath))
            throw new FileNotFoundException("Could not find file.", localPath);
        
        await _adbServerLifetime.ReadyTask;

        var deviceData = ResolveDevice(serial);
        if (deviceData is null)
            throw new InvalidOperationException($"Could not find device with serial: {serial}");

        _logger.LogInformation("Copying file {LocalPath} to {RemotePath}", localPath, remotePath);

        var fileInfo = new FileInfo(localPath);

        progress?.Report(new ProgressInfo
        {
            PercentageComplete = 0,
            Type = ProgressType.FileCopy,
            CurrentFile = Path.GetFileName(localPath),
            TotalBytes = fileInfo.Length,
            Message = "Подготовка к копированию файла..."
        });

        var remoteDir = Path.GetDirectoryName(remotePath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(remoteDir))
        {
            await ExecuteShellCommandAsync(serial, $"mkdir -p \"{remoteDir}\"", cancellationToken);
        }

        _logger.LogInformation("Ensured remote directory: {RemoteDir}", remoteDir);

        await using var fileStream = File.OpenRead(localPath);
        var syncService = new SyncService(_adbClient, deviceData.Value);

        await syncService.PushAsync(
            fileStream,
            remotePath,
            UnixFileStatus.DefaultFileMode,
            DateTime.Now,
            copyProgress =>
            {
                progress?.Report(new ProgressInfo
                {
                    PercentageComplete = (int)copyProgress.ProgressPercentage,
                    BytesTransferred = copyProgress.ReceivedBytesSize,
                    TotalBytes = copyProgress.TotalBytesToReceive,
                    Type = ProgressType.FileCopy,
                    CurrentFile = Path.GetFileName(localPath),
                    Message = $"Копирование: {copyProgress.ProgressPercentage:F1}%"
                });
            },
            cancellationToken);

        progress?.Report(new ProgressInfo
        {
            PercentageComplete = 100,
            Type = ProgressType.FileCopy,
            CurrentFile = Path.GetFileName(localPath),
            BytesTransferred = fileInfo.Length,
            TotalBytes = fileInfo.Length,
            Message = "Копирование завершено"
        });

        _logger.LogInformation("File copied successfully");
    }

    public async Task ExecuteShellCommandAsync(string serial, string command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty.", nameof(command));
        
        await _adbServerLifetime.ReadyTask;

        var deviceData = ResolveDevice(serial);
        if (deviceData is null)
            throw new InvalidOperationException($"Could not find device with serial: {serial}");

        var reciever = new ConsoleOutputReceiver();
        await _adbClient.ExecuteRemoteCommandAsync(command, deviceData.Value, reciever, cancellationToken: cancellationToken);
    }

    private DeviceData? ResolveDevice(string serial) =>
        _adbClient.GetDevices().FirstOrDefault(d => d.Serial == serial);
}