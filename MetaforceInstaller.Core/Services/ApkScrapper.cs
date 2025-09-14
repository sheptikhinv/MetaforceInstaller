using AlphaOmega.Debug;
using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Services;

public static class ApkScrapper
{
    public static ApkInfo GetApkInfo(string apkPath)
    {
        using var apk = new ApkFile(apkPath);
        if (apk is { IsValid: true, AndroidManifest: not null })
        {
            return new ApkInfo(apk.AndroidManifest.Package, apk.AndroidManifest.VersionName,
                apk.AndroidManifest.VersionCode);
        }

        throw new Exception("Invalid APK file");
    }
}