using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace MetaforceInstaller.UI.Controls;

public partial class FilePickerControl : UserControl
{
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<FilePickerControl, string?>(nameof(Path), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<FilePickerControl, string>(nameof(Title), "Choose file");

    public static readonly StyledProperty<string> FileTypeNameProperty =
        AvaloniaProperty.Register<FilePickerControl, string>(nameof(FileTypeName), "All files");

    public static readonly StyledProperty<IReadOnlyList<string>> PatternsProperty =
        AvaloniaProperty.Register<FilePickerControl, IReadOnlyList<string>>(nameof(Patterns), ["*.*"]);

    public static readonly StyledProperty<bool> IsPickingDisabledProperty =
        AvaloniaProperty.Register<FilePickerControl, bool>(nameof(IsPickingDisabled));

    public string? Path { get => GetValue(PathProperty); set => SetValue(PathProperty, value); }
    public string Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string FileTypeName { get => GetValue(FileTypeNameProperty); set => SetValue(FileTypeNameProperty, value); }
    public IReadOnlyList<string> Patterns { get => GetValue(PatternsProperty); set => SetValue(PatternsProperty, value); }
    public bool IsPickingDisabled { get => GetValue(IsPickingDisabledProperty); set => SetValue(IsPickingDisabledProperty, value); }
    public bool ShowTitle => !string.IsNullOrWhiteSpace(Title);

    public FilePickerControl() => InitializeComponent();

    private async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var fileType = new FilePickerFileType(FileTypeName) { Patterns = Patterns };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Title,
            AllowMultiple = false,
            FileTypeFilter = [fileType]
        });

        if (files is [var file, ..])
            Path = file.TryGetLocalPath() ?? file.Path.ToString();
    }
}