using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MetaforceInstaller.UI.Infrastructure;

namespace MetaforceInstaller.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    
    public ViewModelBase? CurrentPage => _navigationService.CurrentPage;
    
    public IReadOnlyList<PageViewModelBase> Pages { get; }
    
    private PageViewModelBase? _selectedPage;
    public PageViewModelBase? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (ReferenceEquals(_selectedPage, value)) return;
            _selectedPage = value;
            RaisePropertyChanged();
            
            if (value != null)
                _navigationService.NavigateTo(value);
        }
    }

    private bool _isNavBarExpanded = true;

    public bool IsNavBarExpanded
    {
        get => _isNavBarExpanded;
        set
        {
            if (_isNavBarExpanded == value) return;
            _isNavBarExpanded = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(NavBarWidth));
        }
    }
    
    private double _windowWidth = 900;
    public double WindowWidth
    {
        get => _windowWidth;
        set
        {
            if (Math.Abs(_windowWidth - value) < 0.1) return;
            _windowWidth = value;
            RaisePropertyChanged();
            
            // ну так-то угар но как-то криво
            // if (value < 900 && IsNavBarExpanded)
            //     IsNavBarExpanded = false;
            // else if (value >= 700 && !IsNavBarExpanded)
            //     IsNavBarExpanded = true;
        }
    }
    
    public double NavBarWidth => IsNavBarExpanded ? 220 : 64;
    
    public string Version { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "";
    
    public ICommand ToggleNavBarCommand { get; }

    public MainWindowViewModel(
        INavigationService navigationService,
        IEnumerable<PageViewModelBase> pages) 
    {
        _navigationService = navigationService;
        
        Pages = pages.ToList();
        
        _navigationService.CurrentPageChanged += (s, e) =>
            RaisePropertyChanged(nameof(CurrentPage));
        
        ToggleNavBarCommand = new RelayCommand(() => IsNavBarExpanded = !IsNavBarExpanded);
        
        SelectedPage = Pages.FirstOrDefault();
    }
}