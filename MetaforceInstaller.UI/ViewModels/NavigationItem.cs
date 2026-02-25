using System;

namespace MetaforceInstaller.UI.ViewModels;

public class NavigationItem
{
    public string Icon { get; }
    public string Title { get; }
    public Type TargetPageType { get; }
    
    public NavigationItem(string icon, string title, Type targetPageType)
    {
        Icon = icon;
        Title = title;
        TargetPageType = targetPageType;
    }
}