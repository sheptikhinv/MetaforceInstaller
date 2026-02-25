namespace MetaforceInstaller.UI.Infrastructure;

public sealed record FilePickerRequest(
    string Title,
    string FileTypeName,
    string[] Patterns,
    bool AllowMultiple = false
);