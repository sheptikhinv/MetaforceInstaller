using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Core.Interfaces;

public interface IDeviceProvider
{
    Task<IReadOnlyList<DeviceInfo>> GetDevicesAsync(CancellationToken cancellationToken = default);
    Task RefreshAsync(CancellationToken cancellationToken = default);
    
    event EventHandler<IReadOnlyList<DeviceInfo>>? DevicesChanged;
    event EventHandler<DeviceInfo>? SelectionChanged;
    
    DeviceInfo? SelectedDevice { get; set; }
    
    Task<bool> TrySelectDeviceAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task ClearSelectionAsync(CancellationToken cancellationToken = default);
    
    bool TryGetSelectedDevice(out DeviceInfo deviceInfo);
    DeviceInfo GetSelectedDeviceOrThrow();
}