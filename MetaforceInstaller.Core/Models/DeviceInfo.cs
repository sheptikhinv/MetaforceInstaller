namespace MetaforceInstaller.Core.Models;

public record DeviceInfo(
    string SerialNumber,
    string State,
    string Model,
    string Name
);