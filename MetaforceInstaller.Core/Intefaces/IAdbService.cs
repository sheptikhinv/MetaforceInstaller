using MetaforceInstaller.Core.Models;
using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Intefaces;

public interface IAdbService
{
    event EventHandler<ProgressInfo>? ProgressChanged;
    event EventHandler<string>? StatusChanged;

    Task InstallApkAsync(string apkPath, IProgress<ProgressInfo>? progress = null, CancellationToken cancellationToken = default);
    Task CopyFileAsync(string localPath, string remotePath, IProgress<ProgressInfo>? progress = null, CancellationToken cancellationToken = default);
    DeviceInfo GetDeviceInfo();

    // Синхронные версии для обратной совместимости
    void InstallApk(string apkPath);
    void CopyFile(string localPath, string remotePath);
}