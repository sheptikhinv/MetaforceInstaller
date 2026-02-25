using System;
using System.Text;

namespace MetaforceInstaller.UI.Logging;

public sealed class LogBuffer
{
    private readonly object _lock = new();
    private readonly StringBuilder _sb = new();

    public event Action? Changed;

    public string Text
    {
        get
        {
            lock (_lock) return _sb.ToString();
        }
    }

    public void AppendLine(string line)
    {
        lock (_lock)
            _sb.AppendLine(line);
        
        Changed?.Invoke();
    }
}