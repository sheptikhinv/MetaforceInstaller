using MetaforceInstaller.Cli.Utils;
using MetaforceInstaller.Core.Services;

namespace MetaforceInstaller.Cli;

static class Program
{
    static void Main(string[] args)
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

            adbService.InstallApk(installationRequest.ApkPath);
            adbService.CopyFile(installationRequest.ZipPath, installationRequest.OutputPath);

            //     var adbPath = ExtractAdbFiles();
            //
            //     var server = new AdbServer();
            //     var result = server.StartServer(adbPath, restartServerIfNewer: false);
            //     Console.WriteLine($"ADB сервер запущен: {result}");
            //
            //     adbClient = new AdbClient();
            //
            //     var devices = adbClient.GetDevices();
            //
            //     if (!devices.Any())
            //     {
            //         Console.WriteLine("Устройства не найдены. Подключите Android-устройство и включите отладку по USB.");
            //         return;
            //     }
            //
            //     deviceData = devices.FirstOrDefault();
            //     Console.WriteLine($"Найдено устройство: {deviceData.Serial}");
            //     Console.WriteLine($"Состояние: {deviceData.State}");
            //     Console.WriteLine($"Имя устройства: {deviceData.Name} - {deviceData.Model}");
            //
            //     InstallApk(installationRequest.ApkPath);
            //     CopyFileToDevice(installationRequest.ZipPath, installationRequest.OutputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
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
        var bytesText = $" {FormatBytes(receivedBytes)} / {FormatBytes(totalBytes)}";

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