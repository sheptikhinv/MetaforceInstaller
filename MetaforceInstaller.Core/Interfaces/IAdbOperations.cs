using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Interfaces;

public interface IAdbOperations
{
    Task InstallApkAsync(
        string serial,
        string apkPath,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);

    Task PushFileAsync(
        string serial,
        string localPath,
        string remotePath,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);

    Task ExecuteShellCommandAsync(
        string serial,
        string command,
        CancellationToken cancellationToken = default);
}