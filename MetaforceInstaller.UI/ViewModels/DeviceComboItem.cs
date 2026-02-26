using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.UI.ViewModels;

public sealed class DeviceComboItem
{
    public string DisplayText { get; }
    public string? SerialNumber { get; }
    public bool IsPlaceholder { get; }

    private DeviceComboItem(string displayText, string? serialNumber, bool isPlaceholder)
    {
        DisplayText = displayText;
        SerialNumber = serialNumber;
        IsPlaceholder = isPlaceholder;
    }

    public static DeviceComboItem NotConnected() =>
        new("Not connected", serialNumber: null, isPlaceholder: true);

    public static DeviceComboItem From(DeviceInfo d)
    {
        var namePart = string.IsNullOrWhiteSpace(d.Name) ? d.Model : d.Name;
        var display = $"{namePart} • {d.State} • {d.SerialNumber}";
        return new DeviceComboItem(display, d.SerialNumber, isPlaceholder: false);
    }

    public override string ToString() => DisplayText;
}