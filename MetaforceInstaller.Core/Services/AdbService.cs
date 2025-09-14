using System.Reflection;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using MetaforceInstaller.Core.Intefaces;
using MetaforceInstaller.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MetaforceInstaller.Core.Services;

public class AdbService : IAdbService
{
    private readonly ILogger<AdbService> _logger;
    private readonly AdbClient _adbClient;
    private DeviceData _deviceData;

    public event EventHandler<ProgressInfo>? ProgressChanged;
    public event EventHandler<string>? StatusChanged;

    public AdbService(ILogger<AdbService>? logger = null)
    {
        _logger = logger ?? new NullLogger<AdbService>();
        var adbPath = GetAdbPath();
        var server = new AdbServer();
        var serverStatus = server.StartServer(adbPath, restartServerIfNewer: false);
        _adbClient = new AdbClient();
        RefreshDeviceData();
    }

    public void RefreshDeviceData()
    {
        var devices = _adbClient.GetDevices();
        _deviceData = devices.FirstOrDefault();
    }

    private void ExtractResource(string resourceName, string outputPath)
    {
        _logger.LogInformation($"Extracting resource: {resourceName} to {outputPath}");
        using var stream = Assembly.GetAssembly(typeof(AdbService)).GetManifestResourceStream(resourceName);
        using var fileStream = File.Create(outputPath);
        stream.CopyTo(fileStream);
        _logger.LogInformation($"Resource extracted: {resourceName} to {outputPath}");
    }

    private string GetAdbPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "MetaforceInstaller", "adb");
        Directory.CreateDirectory(tempDir);

        var adbPath = Path.Combine(tempDir, "adb.exe");

        if (File.Exists(adbPath)) return adbPath;
        ExtractResource("MetaforceInstaller.Core.adb.adb.exe", adbPath);
        ExtractResource("MetaforceInstaller.Core.adb.AdbWinApi.dll", Path.Combine(tempDir, "AdbWinApi.dll"));
        ExtractResource("MetaforceInstaller.Core.adb.AdbWinUsbApi.dll", Path.Combine(tempDir, "AdbWinUsbApi.dll"));

        return adbPath;
    }

    private void OnProgressChanged(ProgressInfo progressInfo)
    {
        ProgressChanged?.Invoke(this, progressInfo);
    }

    private void OnStatusChanged(string status)
    {
        StatusChanged?.Invoke(this, status);
    }

    public void InstallApk(string apkPath)
    {
        InstallApkAsync(apkPath).Wait();
    }

    public async Task InstallApkAsync(string apkPath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(apkPath))
            {
                _logger.LogCritical("Error: Could not find APK file.");
                return;
            }

            OnStatusChanged("Начинаем установку APK...");
            _logger.LogInformation($"Installing APK: {apkPath}");

            progress?.Report(new ProgressInfo
            {
                PercentageComplete = 0,
                Message = "Подготовка к установке APK...",
                Type = ProgressType.Installation,
                CurrentFile = Path.GetFileName(apkPath)
            });

            var packageManager = new PackageManager(_adbClient, _deviceData);

            await Task.Run(() =>
            {
                packageManager.InstallPackage(apkPath, installProgress =>
                {
                    var progressInfo = new ProgressInfo
                    {
                        PercentageComplete = (int)installProgress.UploadProgress,
                        Message = $"Установка APK: {installProgress.UploadProgress:F1}%",
                        Type = ProgressType.Installation,
                        CurrentFile = Path.GetFileName(apkPath)
                    };

                    progress?.Report(progressInfo);
                    OnProgressChanged(progressInfo);
                });
            }, cancellationToken);

            OnStatusChanged("APK успешно установлен!");
            _logger.LogInformation("APK successfully installed!");
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error: {ex.Message}");
            throw;
        }
    }

    public void CopyFile(string localPath, string remotePath)
    {
        CopyFileAsync(localPath, remotePath).Wait();
    }

    public async Task CopyFileAsync(string localPath, string remotePath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(localPath))
            {
                _logger.LogCritical($"Error: Could not find file: {localPath}");
                return;
            }

            OnStatusChanged("Начинаем копирование файла...");
            _logger.LogInformation($"Copying file: {localPath} to {remotePath}");

            var fileInfo = new FileInfo(localPath);

            progress?.Report(new ProgressInfo
            {
                PercentageComplete = 0,
                Message = "Подготовка к копированию файла...",
                Type = ProgressType.FileCopy,
                CurrentFile = Path.GetFileName(localPath),
                TotalBytes = fileInfo.Length
            });

            await Task.Run(() =>
            {
                using var fileStream = File.OpenRead(localPath);
                var syncService = new SyncService(_adbClient, _deviceData);

                syncService.Push(fileStream, remotePath, UnixFileStatus.DefaultFileMode, DateTime.Now,
                    copyProgress =>
                    {
                        var progressInfo = new ProgressInfo
                        {
                            PercentageComplete = (int)copyProgress.ProgressPercentage,
                            BytesTransferred = copyProgress.ReceivedBytesSize,
                            TotalBytes = copyProgress.TotalBytesToReceive,
                            Message = $"Копирование: {copyProgress.ProgressPercentage:F1}%",
                            Type = ProgressType.FileCopy,
                            CurrentFile = Path.GetFileName(localPath)
                        };

                        progress?.Report(progressInfo);
                        OnProgressChanged(progressInfo);
                    });
            }, cancellationToken);

            OnStatusChanged("Файл успешно скопирован!");
            _logger.LogInformation("File successfully copied!");
        }
        catch (Exception ex)
        {
            _logger.LogCritical($"Error: {ex.Message}");
            throw;
        }
    }

    public DeviceInfo GetDeviceInfo()
    {
        return new DeviceInfo(_deviceData.Serial, _deviceData.State.ToString(), _deviceData.Model, _deviceData.Name);
    }
}