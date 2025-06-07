using System;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Abstractions;

namespace VideoConversionApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

    [ObservableProperty]
    public partial MediaSelectionViewModel? MediaSelectionViewModel { get; set; }
    [ObservableProperty]
    public partial ConversionPreviewViewModel? ConversionPreviewViewModel { get; set; }
    [ObservableProperty]
    public partial RenderSettingsViewModel? RenderSettingsViewModel { get; set; }
    
    public MainWindowViewModel(IServiceProvider? serviceProvider)
    {
        MediaSelectionViewModel = serviceProvider?.GetRequiredService<MediaSelectionViewModel>();
        ConversionPreviewViewModel = serviceProvider?.GetRequiredService<ConversionPreviewViewModel>();
        RenderSettingsViewModel = serviceProvider?.GetRequiredService<RenderSettingsViewModel>();
        
        if (Design.IsDesignMode)
        {
            MediaSelectionViewModel = new MediaSelectionViewModel(null!, null!, 
                null!, null!, null!, null!);
            ConversionPreviewViewModel = new ConversionPreviewViewModel(null!, null!, null!);
            RenderSettingsViewModel = new RenderSettingsViewModel();
        }
    }
    
}