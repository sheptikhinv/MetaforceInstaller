using System.Net;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using MetaforceInstaller.Core.Interfaces;
using MetaforceInstaller.Core.Models;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.Core.Services;

public class AdbDeviceProvider : IDeviceProvider, IAsyncDisposable
{
    private readonly ILogger<AdbDeviceProvider> _logger;
    private readonly AdbClient _adbClient;
    private readonly IAdbServerLifetime _adbServerLifetime;

    private readonly SemaphoreSlim _gate = new(1, 1);

    private List<DeviceInfo> _devices = new();
    private DeviceMonitor? _deviceMonitor;
    private bool _monitorStarted;

    public AdbDeviceProvider(ILogger<AdbDeviceProvider> logger, AdbClient adbClient, IAdbServerLifetime adbServerLifetime)
    {
        _logger = logger;
        _adbClient = adbClient;
        _adbServerLifetime = adbServerLifetime;
    }

    public event EventHandler<IReadOnlyList<DeviceInfo>>? DevicesChanged;

    // NOTE: лучше EventHandler<DeviceInfo?>, чтобы можно было прислать null при ClearSelection.
    public event EventHandler<DeviceInfo?>? SelectionChanged;

    private DeviceInfo? SelectedDevice { get; set; }

    DeviceInfo? IDeviceProvider.SelectedDevice
    {
        get => SelectedDevice;
        set => SelectedDevice = value;
    }

    public async Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMonitorStartedAsync(cancellationToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_devices.Count == 0)
            {
                // Ленивая первичная загрузка, чтобы сразу отдать список в UI.
                await RefreshCoreAsync(cancellationToken);
            }

            return _devices.AsReadOnly();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMonitorStartedAsync(cancellationToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await RefreshCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> TrySelectDeviceAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serial number cannot be empty.", nameof(serialNumber));

        await EnsureMonitorStartedAsync(cancellationToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_devices.Count == 0)
            {
                await RefreshCoreAsync(cancellationToken);
            }

            var found = _devices.FirstOrDefault(d => string.Equals(d.SerialNumber, serialNumber, StringComparison.Ordinal));
            if (found is null)
            {
                _logger.LogWarning("Cannot select device {Serial}: not found in current device list.", serialNumber);
                return false;
            }

            if (SelectedDevice is not null &&
                string.Equals(SelectedDevice.SerialNumber, found.SerialNumber, StringComparison.Ordinal))
            {
                return true; // уже выбран
            }

            SelectedDevice = found;
            SelectionChanged?.Invoke(this, SelectedDevice);

            _logger.LogInformation("Selected device: {Serial} ({Model}) [{State}]",
                SelectedDevice.SerialNumber, SelectedDevice.Model, SelectedDevice.State);

            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearSelectionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureMonitorStartedAsync(cancellationToken);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (SelectedDevice is null)
                return;

            SelectedDevice = null;
            SelectionChanged?.Invoke(this, null);
            _logger.LogInformation("Device selection cleared.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool TryGetSelectedDevice(out DeviceInfo deviceInfo)
    {
        deviceInfo = SelectedDevice!;
        return SelectedDevice is not null;
    }

    public DeviceInfo GetSelectedDeviceOrThrow()
    {
        if (SelectedDevice is null)
            throw new InvalidOperationException("No device selected.");

        return SelectedDevice;
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_deviceMonitor is not null)
            {
                try
                {
                    await _deviceMonitor.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error while disposing DeviceMonitor.");
                }

                _deviceMonitor = null;
                _monitorStarted = false;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task EnsureMonitorStartedAsync(CancellationToken cancellationToken)
    {
        if (_monitorStarted)
            return;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_monitorStarted)
                return;
            
            await _adbServerLifetime.ReadyTask;

            // Monitor нужен, чтобы UI автоматически узнавал о подключениях/отключениях.
            // Важно: DeviceMonitor отдаёт "урезанные" DeviceData, поэтому мы на событии просто делаем Refresh.
            var socket = new AdbSocket(new IPEndPoint(IPAddress.Loopback, AdbClient.AdbServerPort));
            _deviceMonitor = new DeviceMonitor(socket);

            _deviceMonitor.DeviceConnected += (_, _) => _ = SafeRefreshFromMonitorAsync();
            _deviceMonitor.DeviceDisconnected += (_, _) => _ = SafeRefreshFromMonitorAsync();
            _deviceMonitor.DeviceChanged += (_, _) => _ = SafeRefreshFromMonitorAsync();

            await _deviceMonitor.StartAsync(cancellationToken);

            _monitorStarted = true;
            _logger.LogInformation("ADB device monitor started.");

            // Первичная синхронизация списка
            await RefreshCoreAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task SafeRefreshFromMonitorAsync()
    {
        try
        {
            // Тут сознательно без CancellationToken: событие мониторинга.
            await RefreshAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Device list refresh failed (monitor event).");
        }
    }

    private async Task RefreshCoreAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deviceDataList = (await _adbClient.GetDevicesAsync()).ToList();

        cancellationToken.ThrowIfCancellationRequested();

        var newList = deviceDataList
            .Select(ToDeviceInfo)
            .OrderBy(d => d.SerialNumber, StringComparer.Ordinal)
            .ToList();

        var listChanged = !AreSameDeviceLists(_devices, newList);
        if (listChanged)
        {
            _devices = newList;
            DevicesChanged?.Invoke(this, _devices.AsReadOnly());

            _logger.LogInformation("Device list updated. Count={Count}", _devices.Count);
        }

        // Если выбранное устройство исчезло — сбрасываем выбор.
        if (SelectedDevice is not null &&
            _devices.All(d => !string.Equals(d.SerialNumber, SelectedDevice.SerialNumber, StringComparison.Ordinal)))
        {
            _logger.LogInformation("Selected device {Serial} is no longer available. Clearing selection.",
                SelectedDevice.SerialNumber);

            SelectedDevice = null;
            SelectionChanged?.Invoke(this, null);
        }
        else if (SelectedDevice is not null)
        {
            // Если устройство осталось, но изменились поля (например State/Name/Model) — обновим ссылку.
            var updated = _devices.FirstOrDefault(d => string.Equals(d.SerialNumber, SelectedDevice.SerialNumber, StringComparison.Ordinal));
            if (updated is not null && !SameDevice(SelectedDevice, updated))
            {
                SelectedDevice = updated;
                SelectionChanged?.Invoke(this, SelectedDevice);
            }
        }
    }

    private static DeviceInfo ToDeviceInfo(DeviceData d)
        => new(d.Serial, d.State.ToString(), d.Model, d.Name);

    private static bool AreSameDeviceLists(List<DeviceInfo> a, List<DeviceInfo> b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a.Count != b.Count)
            return false;

        for (var i = 0; i < a.Count; i++)
        {
            if (!SameDevice(a[i], b[i]))
                return false;
        }

        return true;
    }

    private static bool SameDevice(DeviceInfo x, DeviceInfo y)
    {
        return string.Equals(x.SerialNumber, y.SerialNumber, StringComparison.Ordinal)
               && string.Equals(x.State, y.State, StringComparison.Ordinal)
               && string.Equals(x.Model, y.Model, StringComparison.Ordinal)
               && string.Equals(x.Name, y.Name, StringComparison.Ordinal);
    }
}