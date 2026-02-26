using System;
using MetaforceInstaller.UI.ViewModels;

namespace MetaforceInstaller.UI.Infrastructure;

public interface INavigationService
{
    ViewModelBase? CurrentPage { get; }
    event EventHandler? CurrentPageChanged;
    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo(ViewModelBase viewModel);
    void NavigateTo(Type viewModelType);
}