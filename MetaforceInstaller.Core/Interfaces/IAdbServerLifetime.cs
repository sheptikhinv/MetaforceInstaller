namespace MetaforceInstaller.Core.Interfaces;

public interface IAdbServerLifetime
{
    Task ReadyTask { get; }
    bool IsReady { get; }
}