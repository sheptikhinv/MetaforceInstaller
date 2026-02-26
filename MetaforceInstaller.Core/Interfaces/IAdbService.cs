using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Interfaces;

public interface IAdbService
{
    Task PerformInstallAsync(string apkPath, string localPath, IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);
}