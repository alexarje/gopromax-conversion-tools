using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Abstractions;

namespace VideoConversionApp.Models;

public partial class VideoRenderQueueEntry : ObservableObject
{
    [ObservableProperty]
    public partial IConvertableVideo Video { get; set; }
    [ObservableProperty]
    public partial double Progress { get; set; } = 0;
    [ObservableProperty]
    public partial VideoRenderingState RenderingState { get; set; } = VideoRenderingState.Queued;
    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    public VideoRenderQueueEntry(IConvertableVideo video)
    {
        Video = video;
    }
}