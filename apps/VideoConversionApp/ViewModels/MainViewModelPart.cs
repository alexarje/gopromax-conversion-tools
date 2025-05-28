using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace VideoConversionApp.ViewModels;

public class MainViewModelPart : ViewModelBase
{
    protected IServiceProvider ServiceProvider { get; }
    public MainWindowViewModel MainWindowViewModel => ServiceProvider.GetRequiredService<MainWindowViewModel>();

    protected MainViewModelPart(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    
}