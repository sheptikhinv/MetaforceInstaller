using System.Reflection;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;

namespace MetaforceInstaller.Cli;

class Program
{
    // 1. Получить имя апк и зипки, если не предоставлены - забить дефолтными значениями
    // 2. Распаковать в временную директорию adb (готово)
    // 3. Установить апк
    // 4. Получить имя пакета
    // 5. Сформировать строку пути для контента
    // 6. Копировать зип по сформированному пути

    static AdbClient adbClient;
    static DeviceData deviceData;

    static void Main(string[] args)
    {
        try
        {
            var (apkPath, zipPath, outputPath) = ParseArguments(args);

            if (string.IsNullOrEmpty(apkPath) || string.IsNullOrEmpty(zipPath) || string.IsNullOrEmpty(outputPath))
            {
                ShowUsage();
                return;
            }

            var adbPath = ExtractAdbFiles();

            var server = new AdbServer();
            var result = server.StartServer(adbPath, restartServerIfNewer: false);
            Console.WriteLine($"ADB сервер запущен: {result}");

            adbClient = new AdbClient();

            var devices = adbClient.GetDevices();

            if (!devices.Any())
            {
                Console.WriteLine("Устройства не найдены. Подключите Android-устройство и включите отладку по USB.");
                return;
            }

            deviceData = devices.FirstOrDefault();
            Console.WriteLine($"Найдено устройство: {deviceData.Serial}");
            Console.WriteLine($"Состояние: {deviceData.State}");
            Console.WriteLine($"Имя устройства: {deviceData.Name} - {deviceData.Model}");

            InstallApk(apkPath);
            CopyFileToDevice(zipPath, outputPath);
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


    private static (string? apkPath, string? zipPath, string? outputPath) ParseArguments(string[] args)
    {
        string apkPath = null;
        string zipPath = null;
        string outputPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--apk":
                case "-a":
                    if (i + 1 < args.Length)
                    {
                        apkPath = args[i + 1];
                        i++;
                    }

                    break;

                case "--content":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        zipPath = args[i + 1];
                        i++;
                    }

                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputPath = args[i + 1];
                        i++;
                    }

                    break;

                case "--help":
                case "-h":
                    ShowUsage();
                    Environment.Exit(0);
                    break;
            }
        }

        return (apkPath, zipPath, outputPath);
    }

    private static void ExtractResource(string resourceName, string outputPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        using var fileStream = File.Create(outputPath);
        stream.CopyTo(fileStream);
    }

    private static string ExtractAdbFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "MetaforceInstaller", "adb");
        Directory.CreateDirectory(tempDir);

        var adbPath = Path.Combine(tempDir, "adb.exe");

        if (!File.Exists(adbPath))
        {
            ExtractResource("MetaforceInstaller.Cli.adb.adb.exe", adbPath);
            ExtractResource("MetaforceInstaller.Cli.adb.AdbWinApi.dll", Path.Combine(tempDir, "AdbWinApi.dll"));
            ExtractResource("MetaforceInstaller.Cli.adb.AdbWinUsbApi.dll", Path.Combine(tempDir, "AdbWinUsbApi.dll"));
        }

        return adbPath;
    }

    private static void InstallApk(string apkPath)
    {
        try
        {
            if (!File.Exists(apkPath))
            {
                Console.WriteLine($"APK файл не найден: {apkPath}");
                return;
            }

            Console.WriteLine($"Установка APK: {apkPath}");

            var packageManager = new PackageManager(adbClient, deviceData);
            packageManager.InstallPackage(apkPath, new Action<InstallProgressEventArgs>(o => { }));

            Console.WriteLine("APK успешно установлен!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка установки APK: {ex.Message}");
        }
    }

    private static void CopyFileToDevice(string localPath, string remotePath)
    {
        try
        {
            if (!File.Exists(localPath))
            {
                Console.WriteLine($"Локальный файл не найден: {localPath}");
                return;
            }

            Console.WriteLine($"Копирование файла {localPath} в {remotePath}");

            using var fileStream = File.OpenRead(localPath);
            var syncService = new SyncService(adbClient, deviceData);
            syncService.Push(fileStream, remotePath, UnixFileStatus.DefaultFileMode, DateTime.Now, null);

            Console.WriteLine("Файл успешно скопирован!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка копирования файла: {ex.Message}");
        }
    }
}