using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;

namespace VideoConversionApp.ViewModels.Components;

public partial class VideoPlayerViewModel : ViewModelBase
{

    [ObservableProperty]
    public partial Uri VideoUri { get; set; } 
        
    public VideoPlayerViewModel()
    {
        VideoUri = new Uri("https://streams.videolan.org/streams/360/eagle_360.mp4");
    }
    
}