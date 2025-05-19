using System;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Abstractions;

namespace VideoConversionApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{

    [ObservableProperty]
    private MediaSelectionViewModel? _mediaSelectionViewModel;
    
    public MainViewModel(IServiceProvider? serviceProvider)
    {
        _mediaSelectionViewModel = serviceProvider?.GetRequiredService<MediaSelectionViewModel>();

        if (Design.IsDesignMode)
        {
            _mediaSelectionViewModel = new MediaSelectionViewModel(null!, null!, null!);
        }
    }
    
}