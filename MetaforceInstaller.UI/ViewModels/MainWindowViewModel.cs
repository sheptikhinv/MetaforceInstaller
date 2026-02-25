using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

    public MainWindowViewModel(
        INavigationService navigationService,
        IEnumerable<PageViewModelBase> pages) 
    {
        _navigationService = navigationService;
        
        Pages = pages.ToList();
        
        _navigationService.CurrentPageChanged += (s, e) =>
            RaisePropertyChanged(nameof(CurrentPage));
        
        SelectedPage = Pages.FirstOrDefault();
    }
}