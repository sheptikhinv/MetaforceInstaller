using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using MetaforceInstaller.Core.Intefaces;
using MetaforceInstaller.UI.Infrastructure;
using MetaforceInstaller.UI.Logging;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LogBuffer _logBuffer;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IAdbService _adbService;

    public Interaction<FilePickerRequest, string?> PickFileInteraction { get; } = new();

    public ICommand ChooseApkCommand { get; }
    public ICommand ChooseZipCommand { get; }
    public ICommand InstallCommand { get; }

    private string _apkPath;

    public string? ApkPath
    {
        get => _apkPath;
        private set
        {
            if (_apkPath == value) return;
            _apkPath = value;
            RaisePropertyChanged(nameof(ApkPath));
            RaisePropertyChanged(nameof(CanInstall));
            UpdateCommandStates();
        }
    }

    private string _zipPath;

    public string? ZipPath
    {
        get => _zipPath;
        private set
        {
            if (_zipPath == value) return;
            _zipPath = value;
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

    public bool CanInstall =>
        !IsInstalling &&
        !string.IsNullOrWhiteSpace(ApkPath) &&
        !string.IsNullOrWhiteSpace(ZipPath);

    public bool CanPickFile => !IsInstalling;

    public string LogsText => _logBuffer.Text;

    public string Version { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "";

    public MainWindowViewModel(
        LogBuffer logBuffer,
        ILogger<MainWindowViewModel> logger,
        IAdbService adbService)
    {
        _logBuffer = logBuffer;
        _logger = logger;
        _adbService = adbService;

        _logBuffer.Changed += () =>
            Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(LogsText)));

        ChooseApkCommand = new AsyncCommand(ChooseApkAsync, () => CanPickFile);
        ChooseZipCommand = new AsyncCommand(ChooseZipAsync, () => CanPickFile);
        InstallCommand = new AsyncCommand(InstallAsync, () => CanInstall);

        _logger.LogInformation("MetaforceInstaller started");
    }

    private async Task ChooseApkAsync()
    {
        ApkPath = await PickFileInteraction.HandleAsync(
            new FilePickerRequest(
                Title: "Choose .apk",
                FileTypeName: "APK Files",
                Patterns: ["*.apk"])
        );
        _logger.LogInformation($"Chosen APK path: {ApkPath}");
        RaisePropertyChanged(nameof(ApkPath));
    }

    private async Task ChooseZipAsync()
    {
        ZipPath = await PickFileInteraction.HandleAsync(
            new FilePickerRequest(
                Title: "Choose .zip",
                FileTypeName: "ZIP Files",
                Patterns: ["*.zip"])
        );
        RaisePropertyChanged(nameof(ZipPath));
        _logger.LogInformation($"Chosen ZIP path: {ZipPath}");
    }

    private async Task InstallAsync()
    {
        if (!CanInstall)
            return;

        IsInstalling = true;
        try
        {
            await Task.Delay(500);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private void UpdateCommandStates()
    {
        (ChooseApkCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (ChooseZipCommand as AsyncCommand)?.RaiseCanExecuteChanged();
        (InstallCommand as AsyncCommand)?.RaiseCanExecuteChanged();
    }

    private MainWindowViewModel()
    {
    }
}