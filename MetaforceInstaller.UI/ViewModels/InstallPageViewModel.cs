using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using MetaforceInstaller.Core.Interfaces;
using MetaforceInstaller.Core.Models;
using MetaforceInstaller.UI.Infrastructure;
using MetaforceInstaller.UI.Logging;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.UI.ViewModels;

public partial class InstallPageViewModel : PageViewModelBase
{
    public override string Title => "Install";
    public override string Icon => "CellphoneArrowDownVariant";

    private readonly ILogger<InstallPageViewModel> _logger;
    private readonly IAdbService _adbService;
    private readonly IDeviceProvider _deviceProvider;

    private static readonly DeviceComboItem NotConnectedItem = DeviceComboItem.NotConnected();

    public ICommand InstallCommand { get; }

    public ObservableCollection<DeviceComboItem> DeviceItems { get; } = new();

    private DeviceComboItem? _selectedDeviceItem;

    public DeviceComboItem? SelectedDeviceItem
    {
        get => _selectedDeviceItem;
        set
        {
            if (ReferenceEquals(_selectedDeviceItem, value)) return;
            _selectedDeviceItem = value;
            RaisePropertyChanged(nameof(SelectedDeviceItem));
            UpdateCommandStates();

            if (!_isInitializing)
                _ = ApplyDeviceSelection(value);
        }
    }

    private string? _apkPath;

    public string? ApkPath
    {
        get => _apkPath;
        set
        {
            if (_apkPath == value) return;
            _apkPath = value;
            _logger.LogInformation("Chosen APK path: {Path}", value);
            RaisePropertyChanged(nameof(ApkPath));
            RaisePropertyChanged(nameof(CanInstall));
            UpdateCommandStates();
        }
    }

    private string? _zipPath;

    public string? ZipPath
    {
        get => _zipPath;
        set
        {
            if (_zipPath == value) return;
            _zipPath = value;
            _logger.LogInformation("Chosen ZIP path: {Path}", value);
            RaisePropertyChanged(nameof(ZipPath));
            RaisePropertyChanged(nameof(CanInstall));
            UpdateCommandStates();
        }
    }

    private bool _isInstalling;

    public bool IsInstalling
    {
        get => _isInstalling;
        private set
        {
            if (_isInstalling == value) return;
            _isInstalling = value;
            RaisePropertyChanged(nameof(IsInstalling));
            RaisePropertyChanged(nameof(CanInstall));
            UpdateCommandStates();
        }
    }

    private double _progressValue;

    public double ProgressValue
    {
        get => _progressValue;
        private set
        {
            if (Math.Abs(_progressValue - value) < 0.001) return;
            _progressValue = value;
            RaisePropertyChanged(nameof(ProgressValue));
        }
    }

    private string _progressMessage = "Doing nothing";

    public string ProgressMessage
    {
        get => _progressMessage;
        private set
        {
            if (_progressMessage == value) return;
            _progressMessage = value;
            RaisePropertyChanged(nameof(ProgressMessage));
        }   
    }
    

    public bool CanInstall =>
        !IsInstalling &&
        !string.IsNullOrWhiteSpace(ApkPath) &&
        !string.IsNullOrWhiteSpace(ZipPath) &&
        SelectedDeviceItem is not null &&
        !SelectedDeviceItem.IsPlaceholder;

    private bool _isInitializing = true;

    public InstallPageViewModel(
        ILogger<InstallPageViewModel> logger,
        IAdbService adbService,
        IDeviceProvider deviceProvider)
    {
        _logger = logger;
        _adbService = adbService;
        _deviceProvider = deviceProvider;

        InstallCommand = new AsyncCommand(InstallAsync, () => CanInstall);

        _deviceProvider.DevicesChanged += OnDevicesChanged;
        _deviceProvider.SelectionChanged += OnProviderSelectionChanged;

        DeviceItems.Add(NotConnectedItem);
        SelectedDeviceItem = NotConnectedItem;

        _ = InitializeDevicesAsync();

        _logger.LogInformation("MetaforceInstaller started");
    }

    private async Task InitializeDevicesAsync()
    {
        try
        {
            await _deviceProvider.RefreshAsync();
            var devices = await _deviceProvider.GetDevicesAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isInitializing = false;
                InjectDevicesToUi(devices);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize device list");
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isInitializing = false;
                InjectDevicesToUi(Array.Empty<DeviceInfo>());
            });
        }
    }

    private void OnDevicesChanged(object? sender, IReadOnlyList<DeviceInfo> devices)
        => Dispatcher.UIThread.Post(() => InjectDevicesToUi(devices));

    private void OnProviderSelectionChanged(object? sender, DeviceInfo device)
    {
        if (device is null || string.IsNullOrWhiteSpace(device.SerialNumber))
        {
            Dispatcher.UIThread.Post(() => SelectedDeviceItem = NotConnectedItem);
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            // Уже выбран этот девайс — ничего не делаем
            if (SelectedDeviceItem?.SerialNumber == device.SerialNumber)
                return;

            var match = DeviceItems.FirstOrDefault(x => x.SerialNumber == device.SerialNumber);
            if (match is not null)
                SelectedDeviceItem = match;
        });
    }

    private void InjectDevicesToUi(IReadOnlyList<DeviceInfo> devices)
    {
        var previousSerial = SelectedDeviceItem?.SerialNumber ?? _deviceProvider.SelectedDevice?.SerialNumber;

        DeviceItems.Clear();

        if (devices is null || devices.Count == 0)
        {
            DeviceItems.Add(NotConnectedItem);
            SelectedDeviceItem = NotConnectedItem;
            return;
        }

        foreach (var d in devices)
            DeviceItems.Add(DeviceComboItem.From(d));

        var toSelect =
            (previousSerial is not null
                ? DeviceItems.FirstOrDefault(x => x.SerialNumber == previousSerial)
                : null)
            ?? DeviceItems.FirstOrDefault(x => !x.IsPlaceholder)
            ?? NotConnectedItem;

        SelectedDeviceItem = toSelect;
    }

    private async Task ApplyDeviceSelection(DeviceComboItem? item)
    {
        try
        {
            if (item is null || item.IsPlaceholder || string.IsNullOrWhiteSpace(item.SerialNumber))
            {
                await _deviceProvider.ClearSelectionAsync();
                return;
            }

            // Уже выбран — не дёргаем провайдер лишний раз
            if (_deviceProvider.SelectedDevice?.SerialNumber == item.SerialNumber)
                return;

            var ok = await _deviceProvider.TrySelectDeviceAsync(item.SerialNumber);
            if (!ok)
                _logger.LogInformation("Device selection rejected for serial: {Serial}", item.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply device selection from UI");
        }
    }

    private async Task InstallAsync()
    {
        if (!CanInstall) return;

        IsInstalling = true;
        ProgressValue = 0;

        var uiProgress = new Progress<ProgressInfo>(info =>
            Dispatcher.UIThread.Post(() =>
            {
                ProgressValue = Math.Clamp(info.PercentageComplete, 0, 100);
                ProgressMessage = info.Message ?? "";
            }));

        try
        {
            await _adbService.PerformInstallAsync(ApkPath, ZipPath, uiProgress, default);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private void UpdateCommandStates()
        => (InstallCommand as AsyncCommand)?.RaiseCanExecuteChanged();

    public InstallPageViewModel()
    {
    }
}