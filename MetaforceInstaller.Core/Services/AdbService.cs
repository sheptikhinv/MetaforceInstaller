using System.Reflection;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Logs;
using AdvancedSharpAdbClient.Models;
using MetaforceInstaller.Core.Intefaces;
using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Services;

public class AdbService : IAdbService
{
    private ILogger<AdbService> _logger;
    private readonly AdbClient _adbClient;
    private DeviceData _deviceData;

    public AdbService()
    {
        var adbPath = GetAdbPath();
        var server = new AdbServer();
        var serverStatus = server.StartServer(adbPath, restartServerIfNewer: false);
        _adbClient = new AdbClient();
        var devices = _adbClient.GetDevices();
        _deviceData = devices.FirstOrDefault();
    }

    private void ExtractResource(string resourceName, string outputPath)
    {
        _logger.LogInformation($"Extracting resource: {resourceName} to {outputPath}");
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
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
        ExtractResource("MetaforceInstaller.Cli.adb.adb.exe", adbPath);
        ExtractResource("MetaforceInstaller.Cli.adb.AdbWinApi.dll", Path.Combine(tempDir, "AdbWinApi.dll"));
        ExtractResource("MetaforceInstaller.Cli.adb.AdbWinUsbApi.dll", Path.Combine(tempDir, "AdbWinUsbApi.dll"));

        return adbPath;
    }

    public void InstallApk(string apkPath)
    {
        try
        {
            if (!File.Exists(apkPath))
            {
                _logger.LogCritical("Error: Could not find APK file.");
                return;
            }

            _logger.LogInformation($"Installing APK: {apkPath}");

            var packageManager = new PackageManager(_adbClient, _deviceData);
            packageManager.InstallPackage(apkPath, o => { });

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
        try
        {
            if (!File.Exists(localPath))
            {
                _logger.LogCritical($"Error: Could not find file: {localPath}");
                return;
            }

            _logger.LogInformation($"Copying file: {localPath} to {remotePath}");

            using var fileStream = File.OpenRead(localPath);
            var syncService = new SyncService(_adbClient, _deviceData);

            syncService.Push(fileStream, remotePath, UnixFileStatus.DefaultFileMode, DateTime.Now,
                new Action<SyncProgressChangedEventArgs>(progress => { }));

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