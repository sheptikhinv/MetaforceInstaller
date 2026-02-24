using System.Reflection;
using MetaforceInstaller.Core.Intefaces;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.Core.Services;

public class AdbBinaryProvider : IAdbBinaryProvider
{
    private readonly ILogger<AdbBinaryProvider> _logger;

    public AdbBinaryProvider(ILogger<AdbBinaryProvider> logger)
    {
        _logger = logger;
    }

    // SOURCE PRIORITY
    // 1. Проверить PATH на наличие adb
    // 2. Проверить директорию установщика на наличие adb
    // 3. Вытащить сбандленный adb
    public string GetAdbPath()
    {
        _logger.LogDebug("Looking for ADB binary");
        var fromPath = TryFindInPath();
        if (fromPath is not null)
            return fromPath;

        var fromAppDir = TryFindNearApplication();
        if (fromAppDir is not null)
            return fromAppDir;

        return ExtractBundledToApplicationDirectory();
    }

    private string AdbFileName =>
        OperatingSystem.IsWindows() ? "adb.exe" : "adb";

    private IEnumerable<string> CandidateAdbFileNames()
    {
        if (OperatingSystem.IsWindows())
            return new[] { "adb.exe", "adb" };

        return new[] { "adb" };
    }

    private string? TryFindInPath()
    {
        _logger.LogDebug("Looking for ADB binary in PATH");
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = dir.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            foreach (var fileName in CandidateAdbFileNames())
            {
                var candidate = Path.Combine(trimmed, fileName);
                if (File.Exists(candidate))
                {
                    _logger.LogDebug($"Found ADB binary in PATH: {candidate}");
                    return candidate;
                }
            }
        }

        _logger.LogDebug("No ADB binary found in PATH");
        return null;
    }

    private string? TryFindNearApplication()
    {
        _logger.LogDebug("Looking for ADB binary near application");
        var baseDir = AppContext.BaseDirectory;

        var candidates = new[]
        {
            Path.Combine(baseDir, AdbFileName),
            Path.Combine(baseDir, "adb", AdbFileName),
            Path.Combine(baseDir, "tools", "adb", AdbFileName),
            Path.Combine(baseDir, "platform-tools", AdbFileName),
        };

        foreach (var candidate in candidates)
        {
            _logger.LogDebug($"Looking for ADB binary near application: {candidate}");
            if (File.Exists(candidate))
            {
                _logger.LogDebug($"Found ADB binary near application: {candidate}");
                return candidate;
            }
        }

        _logger.LogDebug("No ADB binary found near application");
        return null;
    }

    private string ExtractBundledToApplicationDirectory()
    {
        _logger.LogDebug("Extracting bundled ADB binary to application directory");
        var baseDir = AppContext.BaseDirectory;
        var adbDir = Path.Combine(baseDir, "adb");
        Directory.CreateDirectory(adbDir);

        var adbPath = Path.Combine(adbDir, AdbFileName);

        if (File.Exists(adbPath))
        {
            EnsureExecutable(adbPath);
            return adbPath;
        }

        var asm = Assembly.GetAssembly(typeof(AdbBinaryProvider)) ?? Assembly.GetExecutingAssembly();

        var resourceCandidate = OperatingSystem.IsWindows()
            ? "MetaforceInstaller.Core.adb.adb.exe"
            : "MetaforceInstaller.Core.adb.adb";

        Stream? stream = null;
        stream = asm.GetManifestResourceStream(resourceCandidate);

        if (stream is null)
        {
            var available = asm.GetManifestResourceNames();
            throw new FileNotFoundException(
                $"Bundled ADB resource not found. " +
                $"Available resources: {string.Join(", ", available)}");
        }

        using (stream)
        using (var fileStream = File.Create(adbPath))
        {
            stream.CopyTo(fileStream);
        }

        EnsureExecutable(adbPath);
        return adbPath;
    }

    private void EnsureExecutable(string filePath)
    {
        if (OperatingSystem.IsWindows())
            return;

        try
        {
            var mode = File.GetUnixFileMode(filePath);
            mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(filePath, mode);
        }
        catch
        {
            // ignore
        }
    }
}