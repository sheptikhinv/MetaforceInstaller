using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Interfaces;

public interface IAdbService
{
    Task InstallApkAsync(string apkPath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);

    Task CopyFileAsync(string localPath, string remotePath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);

    DeviceInfo GetDeviceInfo();

    Task PerformInstallAsync(string apkPath, string localPath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);
}