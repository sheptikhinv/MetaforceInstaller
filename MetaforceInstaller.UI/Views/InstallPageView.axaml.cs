using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MetaforceInstaller.UI.Infrastructure;
using MetaforceInstaller.UI.ViewModels;

namespace MetaforceInstaller.UI.Views;

public partial class InstallPageView : UserControl
{
    IDisposable? _selectFileInteractionDisposable;

    public InstallPageView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        _selectFileInteractionDisposable?.Dispose();

        if (DataContext is InstallPageViewModel vm)
        {
            _selectFileInteractionDisposable = vm.PickFileInteraction.RegisterHandler(InteractionHandler);
        }
    }

    private async Task<string?> InteractionHandler(FilePickerRequest input)
    {
        var topLevel = TopLevel.GetTopLevel(this);

        var storageFile = await topLevel!.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions()
            {
                AllowMultiple = input.AllowMultiple,
                Title = input.Title,
                FileTypeFilter =
                [
                    new FilePickerFileType(input.FileTypeName)
                    {
                        Patterns = input.Patterns
                    }
                ]
            });
        return storageFile.Count >= 1 ? storageFile[0].Path.LocalPath : null;
    }
}