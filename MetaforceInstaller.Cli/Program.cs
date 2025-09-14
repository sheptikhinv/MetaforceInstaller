using MetaforceInstaller.Cli.Utils;
using MetaforceInstaller.Core.Services;

namespace MetaforceInstaller.Cli;

static class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var installationRequest = ArgumentParser.ParseArguments(args);

            if (installationRequest is null ||
                string.IsNullOrEmpty(installationRequest.ApkPath) ||
                string.IsNullOrEmpty(installationRequest.ZipPath) ||
                string.IsNullOrEmpty(installationRequest.OutputPath))
            {
                ShowUsage();
                return;
            }

            var adbService = new AdbService();

            // Подписка на события прогресса
            adbService.ProgressChanged += OnProgressChanged;
            adbService.StatusChanged += OnStatusChanged;

            // Получение информации об устройстве
            var deviceInfo = adbService.GetDeviceInfo();
            Console.WriteLine($"Найдено устройство: {deviceInfo.SerialNumber}");
            Console.WriteLine($"Состояние: {deviceInfo.State}");
            Console.WriteLine($"Модель: {deviceInfo.Model} - {deviceInfo.Name}");
            Console.WriteLine();

            // Создание объекта для отслеживания прогресса
            var progress = new Progress<MetaforceInstaller.Core.Models.ProgressInfo>(OnProgressReport);

            // Установка APK
            await adbService.InstallApkAsync(installationRequest.ApkPath, progress);
            Console.WriteLine();

            // Копирование файла
            await adbService.CopyFileAsync(installationRequest.ZipPath, installationRequest.OutputPath, progress);
            Console.WriteLine();

            Console.WriteLine("Операция завершена успешно!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    private static void OnProgressChanged(object? sender, MetaforceInstaller.Core.Models.ProgressInfo e)
    {
        DrawProgressBar(e.PercentageComplete, e.BytesTransferred, e.TotalBytes);
    }

    private static void OnStatusChanged(object? sender, string e)
    {
        Console.WriteLine(e);
    }

    private static void OnProgressReport(MetaforceInstaller.Core.Models.ProgressInfo progressInfo)
    {
        if (progressInfo.TotalBytes > 0)
        {
            DrawProgressBar(progressInfo.PercentageComplete, progressInfo.BytesTransferred, progressInfo.TotalBytes);
        }
        else
        {
            // Для случаев без информации о байтах (например, установка APK)
            DrawProgressBar(progressInfo.PercentageComplete, 0, 100);
        }
    }

    static void ShowUsage()
    {
        Console.WriteLine("Использование:");
        Console.WriteLine(
            "  MetaforceInstaller.exe --apk <путь_к_apk> --content <путь_к_zip> --output <путь_для контента>");
        Console.WriteLine("  MetaforceInstaller.exe -a <путь_к_apk> -c <путь_к_zip> -o <путь_для_контента>");
        Console.WriteLine();
        Console.WriteLine("Параметры:");
        Console.WriteLine("  --apk, -a      Путь к APK файлу");
        Console.WriteLine("  --content, -c  Путь к ZIP файлу с контентом");
        Console.WriteLine("  --output, -o   Путь для копирования контента");
        Console.WriteLine("  --help, -h     Показать эту справку");
        Console.WriteLine();
        Console.WriteLine("Пример:");
        Console.WriteLine(
            "  MetaforceInstaller.exe --apk \"C:\\app.apk\" --content \"C:\\data.zip\" --output \"/sdcard/data.zip\"");
        Console.WriteLine("  MetaforceInstaller.exe -a app.apk -c data.zip -o /sdcard/data.zip");
    }

    private static void DrawProgressBar(int progress, long receivedBytes, long totalBytes)
    {
        Console.SetCursorPosition(0, Console.CursorTop);

        var barLength = 40;
        var filledLength = (int)(barLength * progress / 100.0);

        var bar = "[" + new string('█', filledLength) + new string('░', barLength - filledLength) + "]";

        string bytesText = "";
        if (totalBytes > 0)
        {
            bytesText = $" {FormatBytes(receivedBytes)} / {FormatBytes(totalBytes)}";
        }

        Console.Write($"\r{bar} {progress}%{bytesText}");
    }

    private static string FormatBytes(long bytes)
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
}