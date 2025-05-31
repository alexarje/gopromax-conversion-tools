using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace VideoConversionApp.ViewModels;

/// <summary>
/// Signifies the ViewModel is a part of the MainViewModel in a sense.
/// The UI is split to multiple UserControls and ViewModels, but spiritually they are a part of the same main
/// view, and they communicate with each other and so cross-ViewModel access is provided by this arrangement.  
/// </summary>
public abstract class MainViewModelPart : ViewModelBase
{
    protected IServiceProvider ServiceProvider { get; }
    public MainWindowViewModel MainWindowViewModel => ServiceProvider.GetRequiredService<MainWindowViewModel>();

    protected MainViewModelPart(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    
}