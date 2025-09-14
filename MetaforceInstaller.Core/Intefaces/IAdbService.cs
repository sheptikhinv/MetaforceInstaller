using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Intefaces;

public interface IAdbService
{
    public void InstallApk(string apkPath);
    public void CopyFile(string localPath, string remotePath);
    public DeviceInfo GetDeviceInfo();
}