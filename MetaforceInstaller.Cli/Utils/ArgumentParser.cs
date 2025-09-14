using MetaforceInstaller.Core.Models;

namespace MetaforceInstaller.Cli.Utils;

public static class ArgumentParser
{
    public static InstallationRequest? ParseArguments(string[] args)
    {
        var result = new InstallationRequest();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--apk":
                case "-a":
                    if (i + 1 < args.Length)
                    {
                        result.ApkPath = args[i + 1];
                        i++;
                    }

                    break;

                case "--content":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        result.ZipPath = args[i + 1];
                        i++;
                    }

                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        result.OutputPath = args[i + 1];
                        i++;
                    }

                    break;

                case "--help":
                case "-h":
                    return null;
            }
        }

        return result;
    }
}