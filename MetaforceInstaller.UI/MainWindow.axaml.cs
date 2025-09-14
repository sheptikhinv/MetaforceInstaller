using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace MetaforceInstaller.UI;

public partial class MainWindow : Window
{
    private string? _apkPath;
    private string? _zipPath;

    public MainWindow()
    {
        InitializeComponent();
        CheckAndEnableInstallButton();

        ChooseApkButton.Click += OnChooseApkClicked;
        ChooseContentButton.Click += OnChooseContentClicked;
        InstallButton.Click += OnInstallClicked;
    }

    private async void CheckAndEnableInstallButton()
    {
        InstallButton.IsEnabled = !string.IsNullOrEmpty(_apkPath) && !string.IsNullOrEmpty(_zipPath);
    }

    private async void OnChooseApkClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите APK файл",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("APK Files")
                {
                    Patterns = new[] { "*.apk" }
                }
            }
        });

        if (files.Count >= 1)
        {
            _apkPath = files[0].Path.LocalPath;
            LogMessage($"APK выбран: {Path.GetFileName(_apkPath)}");
        }

        CheckAndEnableInstallButton();
    }

    private async void OnChooseContentClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите архив с контентом",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("ZIP Files")
                {
                    Patterns = new[] { "*.zip" }
                }
            }
        });

        if (files.Count >= 1)
        {
            _zipPath = files[0].Path.LocalPath;
            LogMessage($"Контент выбран: {Path.GetFileName(_zipPath)}");
        }

        CheckAndEnableInstallButton();
    }

    private async void OnInstallClicked(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_apkPath) || string.IsNullOrEmpty(_zipPath))
        {
            LogMessage("Ошибка: Выберите APK файл и папку с контентом");
            return;
        }

        InstallButton.IsEnabled = false;
        InstallProgressBar.Value = 0;

        try
        {
            LogMessage("Начинаем установку...");

            // Здесь будет ваша логика установки
            await SimulateInstallation();

            LogMessage("Установка завершена успешно!");
        }
        catch (Exception ex)
        {
            LogMessage($"Ошибка установки: {ex.Message}");
        }
        finally
        {
            InstallButton.IsEnabled = true;
            InstallProgressBar.Value = 0;
        }
    }

    private void LogMessage(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogsTextBox.Text += $"[{timestamp}] {message}\n";

        // Прокручиваем к концу
        var scrollViewer = LogsTextBox.FindAncestorOfType<ScrollViewer>();
        scrollViewer?.ScrollToEnd();
    }

    private async Task SimulateInstallation()
    {
        for (int i = 0; i <= 100; i += 10)
        {
            InstallProgressBar.Value = i;
            LogMessage($"Прогресс: {i}%");
            await Task.Delay(500); // Симуляция работы
        }
    }
}