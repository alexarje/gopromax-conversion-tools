using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using VideoConversionApp.Models;
using VideoConversionApp.Views.Components;

namespace VideoConversionApp.ViewModels.Components;

/// <summary>
/// Model for the VideoPlayerView.
/// The VideoView does not play nice with bindings; they break probably because of
/// how the control kinds detaches to its own window and values do not seem to update correctly
/// to the controls inside the view.
/// This is why there is so much logic in the view's code-behind.
/// </summary>
public partial class VideoPlayerViewModel : ViewModelBase
{

    [ObservableProperty]
    public partial Uri? VideoUri { get; set; } = null;

    public ConvertibleVideoModel? SourceConvertibleVideo { get; set; } = null;
    public KeyFrameVideo KeyFrameVideo { get; set; } = null!;
    
    // For parent viewmodel needs, actually. This is a bit of a mess.
    public VideoPlayerView AssociatedView { get; set; } = null!; 
    
    
    // "Read only" properties; set from the VideoPlayerView.
    // The parent ViewModel can listen to these to sync its control-bound properties to
    // the same values.
    [ObservableProperty]
    public partial float VideoPlayerYaw { get; set; }
    [ObservableProperty]
    public partial float VideoPlayerPitch { get; set; }
    [ObservableProperty]
    public partial float VideoPlayerRoll { get; set; }
    [ObservableProperty]
    public partial float VideoPlayerFov { get; set; }
    [ObservableProperty]
    public partial decimal CropTimelineStartTime { get; set; }
    [ObservableProperty]
    public partial decimal CropTimelineEndTime { get; set; } 
    
    public VideoPlayerViewModel(ConvertibleVideoModel? sourceConvertibleVideo, KeyFrameVideo? video)
    {
        SourceConvertibleVideo = sourceConvertibleVideo;
        if (video != null)
        {
            KeyFrameVideo = video;
            VideoUri = new Uri(video.VideoPath);
        }
    }
    
    
}