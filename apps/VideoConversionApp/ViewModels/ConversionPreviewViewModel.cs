using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.ViewModels;

public partial class ConversionPreviewViewModel : MainViewModelPart
{
    private readonly IMediaPreviewService _mediaPreviewService;

    [ObservableProperty]
    public partial ConvertibleVideoModel? VideoModel { get; set; }
    
    public List<Bitmap> SnapshotFrameImages { get; set; } = new List<Bitmap>();
    
    [ObservableProperty]
    public partial Bitmap? CurrentSnapshotFrameImage { get; set; }
    
    
    public ConversionPreviewViewModel(IServiceProvider serviceProvider,
        IMediaPreviewService mediaPreviewService) : base(serviceProvider)
    {
        _mediaPreviewService = mediaPreviewService;
    }

    public async Task SetVideoModelAsync(ConvertibleVideoModel? videoModel)
    {
        VideoModel = videoModel;
        // TODO generate thumbnails
        
        // TODO if null, load blank image
        if (videoModel == null)
            return;
        
        var frames = await _mediaPreviewService.GenerateSnapshotFramesAsync(videoModel.MediaInfo, 5);
        var bitmaps = new List<Bitmap>();

        foreach (var frame in frames)
        {
            using var stream = new MemoryStream(frame);
            bitmaps.Add(new Bitmap(stream));
        }
        
        SnapshotFrameImages = bitmaps;
        NextFrame();
    }

    [RelayCommand]
    public void NextFrame()
    {
        if (SnapshotFrameImages.Count == 0)
            return;
        
        var currentIndex = SnapshotFrameImages.IndexOf(CurrentSnapshotFrameImage);
        var nextIndex = currentIndex + 1;
        if (nextIndex >= SnapshotFrameImages.Count)
            nextIndex = 0;
        CurrentSnapshotFrameImage = SnapshotFrameImages[nextIndex];
    }

    [RelayCommand]
    public void PreviousFrame()
    {
        if (SnapshotFrameImages.Count == 0)
            return;
        
        var currentIndex = SnapshotFrameImages.IndexOf(CurrentSnapshotFrameImage);
        var prevIndex = currentIndex - 1;
        if (prevIndex < 0)
            prevIndex = SnapshotFrameImages.Count - 1;
        CurrentSnapshotFrameImage = SnapshotFrameImages[prevIndex];
    }
}