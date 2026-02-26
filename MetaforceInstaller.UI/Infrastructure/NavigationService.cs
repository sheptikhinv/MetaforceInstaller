using System;
using MetaforceInstaller.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MetaforceInstaller.UI.Infrastructure;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ViewModelBase? _currentPage;

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (ReferenceEquals(_currentPage, value)) return;
            _currentPage = value;
            CurrentPageChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? CurrentPageChanged;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var vm = _serviceProvider.GetRequiredService<TViewModel>();
        NavigateTo(vm);
    }

    public void NavigateTo(ViewModelBase viewModel)
    {
        CurrentPage = viewModel;
    }

    public void NavigateTo(Type viewModelType)
    {
        var vm = (ViewModelBase)_serviceProvider.GetRequiredService(viewModelType);
        NavigateTo(vm);
    }
}