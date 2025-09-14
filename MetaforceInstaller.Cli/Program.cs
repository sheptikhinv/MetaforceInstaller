using System.Reflection;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;

namespace MetaforceInstaller.Cli;

class Program
{
    static AdbClient adbClient;
    static DeviceData deviceData;
    
    static void Main(string[] args)
    {
        try
        {
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
            
            // Пример установки APK
            // InstallApk("path/to/your/app.apk");
            
            // Пример копирования файла
            // CopyFileToDevice("path/to/your/file.zip", "/sdcard/file.zip");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }

    static void ExtractResource(string resourceName, string outputPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        using var fileStream = File.Create(outputPath);
        stream.CopyTo(fileStream);
    }

    static string ExtractAdbFiles()
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
    
    static void InstallApk(string apkPath)
    {
        throw new NotImplementedException();
        // try
        // {
        //     if (!File.Exists(apkPath))
        //     {
        //         Console.WriteLine($"APK файл не найден: {apkPath}");
        //         return;
        //     }
        //
        //     Console.WriteLine($"Установка APK: {apkPath}");
        //     
        //     using var apkStream = File.OpenRead(apkPath);
        //     var packageManager = new PackageManager(adbClient, deviceData);
        //     packageManager.InstallPackage(apkStream, "temp.apk");
        //     
        //     Console.WriteLine("APK успешно установлен!");
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Ошибка установки APK: {ex.Message}");
        // }
    }
    
    static void CopyFileToDevice(string localPath, string remotePath)
    {
        throw new NotImplementedException();
        // try
        // {
        //     if (!File.Exists(localPath))
        //     {
        //         Console.WriteLine($"Локальный файл не найден: {localPath}");
        //         return;
        //     }
        //
        //     Console.WriteLine($"Копирование файла {localPath} в {remotePath}");
        //     
        //     using var fileStream = File.OpenRead(localPath);
        //     var syncService = new SyncService(adbClient, deviceData);
        //     syncService.Push(fileStream, remotePath, UnixFileStatus.DefaultFileMode, DateTime.Now, null);
        //     
        //     Console.WriteLine("Файл успешно скопирован!");
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Ошибка копирования файла: {ex.Message}");
        // }
    }
}