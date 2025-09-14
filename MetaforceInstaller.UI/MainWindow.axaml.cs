using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MetaforceInstaller.Core.Models;
using MetaforceInstaller.Core.Services;

namespace MetaforceInstaller.UI;

public partial class MainWindow : Window
{
    private string? _apkPath;
    private string? _zipPath;
    private AdbService _adbService;

    private const int PROGRESS_LOG_STEP = 10;
    private const int PROGRESS_UPDATE_STEP = 1;

    private int _lastLoggedProgress = -1;
    private int _lastUpdatedProgress = -1;

    public MainWindow()
    {
        InitializeComponent();

        LogMessage("SLAVAGM ЛЕГЕНДА И ВЫ ЭТО ЗНАЕТЕ");

        _adbService = new AdbService();
        _adbService.ProgressChanged += OnAdbProgressChanged;
        _adbService.StatusChanged += OnAdbStatusChanged;

        CheckAndEnableInstallButton();

        ChooseApkButton.Click += OnChooseApkClicked;
        ChooseContentButton.Click += OnChooseContentClicked;
        InstallButton.Click += OnInstallClicked;
    }

    private void OnAdbProgressChanged(object? sender, ProgressInfo e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (e.PercentageComplete != _lastUpdatedProgress &&
                e.PercentageComplete % PROGRESS_UPDATE_STEP == 0)
            {
                InstallProgressBar.Value = e.PercentageComplete;
                _lastUpdatedProgress = e.PercentageComplete;
            }

            if (e.PercentageComplete != _lastLoggedProgress &&
                e.PercentageComplete % PROGRESS_LOG_STEP == 0 || e.PercentageComplete == 100)
            {
                LogMessage(
                    e.TotalBytes > 0
                        ? $"Прогресс: {e.PercentageComplete}% ({FormatBytes(e.BytesTransferred)} / {FormatBytes(e.TotalBytes)})"
                        : $"Прогресс: {e.PercentageComplete}%");

                _lastLoggedProgress = e.PercentageComplete;
            }
        });
    }

    private void OnProgressReport(ProgressInfo progressInfo)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (progressInfo.PercentageComplete != _lastUpdatedProgress &&
                progressInfo.PercentageComplete % PROGRESS_UPDATE_STEP == 0)
            {
                InstallProgressBar.Value = progressInfo.PercentageComplete;
                _lastUpdatedProgress = progressInfo.PercentageComplete;
            }


            if (progressInfo.PercentageComplete != _lastLoggedProgress &&
                (progressInfo.PercentageComplete % PROGRESS_LOG_STEP == 0 || progressInfo.PercentageComplete == 100))
            {
                LogMessage(
                    progressInfo.TotalBytes > 0
                        ? $"Прогресс: {progressInfo.PercentageComplete}% ({FormatBytes(progressInfo.BytesTransferred)} / {FormatBytes(progressInfo.TotalBytes)})"
                        : $"Прогресс: {progressInfo.PercentageComplete}%");

                _lastLoggedProgress = progressInfo.PercentageComplete;
            }
        });
    }

    private void OnAdbStatusChanged(object? sender, string e)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            InstallProgressBar.Value = 0;
            _lastLoggedProgress = -1;
            _lastUpdatedProgress = -1;
            LogMessage(e);
        });
    }

    private string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        var counter = 0;
        double number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:N1} {suffixes[counter]}";
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

        _adbService.RefreshDeviceData();

        InstallButton.IsEnabled = false;
        InstallProgressBar.Value = 0;

        try
        {
            LogMessage("Начинаем установку...");

            var deviceInfo = _adbService.GetDeviceInfo();
            LogMessage($"Найдено устройство: {deviceInfo.SerialNumber}");
            LogMessage($"Состояние: {deviceInfo.State}");
            LogMessage($"Модель: {deviceInfo.Model} - {deviceInfo.Name}");

            var progress = new Progress<ProgressInfo>(OnProgressReport);

            await _adbService.InstallApkAsync(_apkPath, progress);

            var apkInfo = ApkScrapper.GetApkInfo(_apkPath);
            LogMessage($"Ставим {apkInfo.PackageName} версии {apkInfo.VersionName}");
            var zipName = Path.GetFileName(_zipPath);
            var outputPath =
                @$"/storage/emulated/0/Android/data/{apkInfo.PackageName}/files/{zipName}";
            LogMessage($"Начинаем копирование контента в {outputPath}");

            await _adbService.CopyFileAsync(_zipPath, outputPath, progress);

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

        var scrollViewer = LogsTextBox.FindAncestorOfType<ScrollViewer>();
        scrollViewer?.ScrollToEnd();
    }
}