using AdvancedSharpAdbClient;
using MetaforceInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.Core.Services;

public class AdbServerController : IAdbServerController
{
    private readonly ILogger<AdbServerController> _logger;
    private readonly IAdbBinaryProvider _adbBinaryProvider;

    public AdbServerController(ILogger<AdbServerController> logger, IAdbBinaryProvider adbBinaryProvider)
    {
        _logger = logger;
        _adbBinaryProvider = adbBinaryProvider;
    }

    public async Task StartAdbServerAsync()
    {
        var adbPath = _adbBinaryProvider.GetAdbPath();
        if (adbPath == null)
        {
            throw new InvalidOperationException("Could not find ADB binary");
        }

        var adbServer = new AdbServer();
        await adbServer.StartServerAsync(adbPath, restartServerIfNewer: false);
        _logger.LogInformation("ADB server started");
    }
}