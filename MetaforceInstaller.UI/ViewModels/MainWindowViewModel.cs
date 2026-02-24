using System;
using System.Reflection;
using Avalonia.Threading;
using MetaforceInstaller.Core.Intefaces;
using MetaforceInstaller.UI.Logging;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly LogBuffer _logBuffer;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IAdbService _adbService;

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

        _logger.LogInformation("MetaforceInstaller started");
    }

    public MainWindowViewModel()
    {
        
    }
}