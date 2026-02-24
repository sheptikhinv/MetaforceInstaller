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

    public string? ApkPath { get; private set; }
    public string? ZipPath { get; private set; }


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

        ChooseApkCommand = new AsyncCommand(ChooseApkAsync);
        ChooseZipCommand = new AsyncCommand(ChooseZipAsync);

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
    }

    private MainWindowViewModel()
    {
    }
}