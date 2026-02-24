using MetaforceInstaller.Core.Intefaces;
using MetaforceInstaller.Core.Services;
using MetaforceInstaller.UI.Logging;
using MetaforceInstaller.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MetaforceInstaller.UI.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection services)
    {
        // UI log sink (will be bound to TextBox via VM)
        services.AddSingleton<LogBuffer>();
        services.AddSingleton<IAdbBinaryProvider, AdbBinaryProvider>();
        services.AddSingleton<IAdbService, AdbService>();

        // Plug LogBuffer into Microsoft.Extensions.Logging pipeline
        services.AddSingleton<ILoggerProvider, LogBufferLoggerProvider>();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddConsole();
            // Providers are taken from DI (including LogBufferLoggerProvider above)
        });

        services.AddSingleton<MainWindowViewModel>();
        // ... register other services here (IAdbService, view models, etc.)
    }
}