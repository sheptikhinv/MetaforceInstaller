using Avalonia.Threading;
using MetaforceInstaller.UI.Logging;

namespace MetaforceInstaller.UI.ViewModels;

public class LogsPageViewModel : PageViewModelBase
{
    public override string Title => "Logs";
    public override string Icon => "NoteOutline";

    private readonly LogBuffer _logBuffer;

    public string LogsText => _logBuffer.Text;

    public LogsPageViewModel(LogBuffer logBuffer)
    {
        _logBuffer = logBuffer;

        _logBuffer.Changed += () =>
            Dispatcher.UIThread.Post(() => RaisePropertyChanged(nameof(LogsText)));
    }
}