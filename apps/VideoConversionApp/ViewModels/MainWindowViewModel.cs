using System;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{

    [ObservableProperty]
    public partial MediaSelectionViewModel? MediaSelectionViewModel { get; set; }
    [ObservableProperty]
    public partial ConversionPreviewViewModel? ConversionPreviewViewModel { get; set; }
    [ObservableProperty]
    public partial RenderSettingsViewModel? RenderSettingsViewModel { get; set; }
    [ObservableProperty]
    public partial RenderQueueViewModel? RenderQueueViewModel { get; set; }
    
    public MainWindowViewModel(IServiceProvider? serviceProvider)
    {
        MediaSelectionViewModel = serviceProvider?.GetRequiredService<MediaSelectionViewModel>();
        ConversionPreviewViewModel = serviceProvider?.GetRequiredService<ConversionPreviewViewModel>();
        RenderSettingsViewModel = serviceProvider?.GetRequiredService<RenderSettingsViewModel>();
        RenderQueueViewModel = serviceProvider?.GetRequiredService<RenderQueueViewModel>();
        
        if (Design.IsDesignMode)
        {
            ConversionPreviewViewModel = new ConversionPreviewViewModel(null!, null!, null!, new PreviewVideoPlayerState());
            MediaSelectionViewModel = new MediaSelectionViewModel(null!, null!, 
                null!, null!, null!, ConversionPreviewViewModel);
            RenderSettingsViewModel = new RenderSettingsViewModel(null!, null!);
            RenderQueueViewModel = new RenderQueueViewModel();
        }
    }
    
}