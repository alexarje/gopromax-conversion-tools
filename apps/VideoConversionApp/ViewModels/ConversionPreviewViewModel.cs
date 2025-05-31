using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.ViewModels;

public partial class ConversionPreviewViewModel : MainViewModelPart
{
    private readonly IMediaPreviewService _mediaPreviewService;
    private readonly IAppSettingsService _appSettingsService;

    [ObservableProperty]
    public partial ConvertibleVideoModel? VideoModel { get; set; }
    
    public List<IImage> SnapshotFrameImages { get; set; } = new List<IImage>();
    
    [ObservableProperty]
    public partial IImage? CurrentSnapshotFrameImage { get; set; }
    [ObservableProperty]
    public partial double SnapshotRenderProgress { get; set; }
    [ObservableProperty]
    public partial bool BlurImageVisible { get; set; }
    
    private CancellationTokenSource? _snapshotGenerationCts;
    
    public ConversionPreviewViewModel(IServiceProvider serviceProvider,
        IMediaPreviewService mediaPreviewService,
        IAppSettingsService appSettingsService) : base(serviceProvider)
    {
        _mediaPreviewService = mediaPreviewService;
        _appSettingsService = appSettingsService;
    }

    public async Task SetActiveVideoModelAsync(ConvertibleVideoModel? videoModel, IImage? initialPreviewImage)
    {

        BlurImageVisible = true;
        
        // If there's already this operation in progress, cancel the previous one.
        if (_snapshotGenerationCts != null && !_snapshotGenerationCts.Token.IsCancellationRequested)
        {
            _snapshotGenerationCts.Cancel();
            Console.WriteLine("CANCEL!");
        }

        var myCts = new CancellationTokenSource();
        _snapshotGenerationCts = myCts;
        
        VideoModel = videoModel;
        
        if (videoModel == null && initialPreviewImage == null)
            return;

        if (initialPreviewImage != null)
        {
            CurrentSnapshotFrameImage = initialPreviewImage;
            SnapshotFrameImages = new List<IImage>
            {
                initialPreviewImage
            };
        }

        if (videoModel == null)
            return;

        var frameCount = _appSettingsService.GetSettings().NumberOfSnapshotFrames;
        
        // Clear the queue if it's currently generating something already.
        // _mediaPreviewService.ClearSnapshotFrameQueue();

        try
        {
            var bitmapBytes = await _mediaPreviewService.GenerateSnapshotFramesAsync(videoModel.MediaInfo, frameCount,
                (progress) => SnapshotRenderProgress = progress, myCts.Token);

            var bitmaps = new IImage[bitmapBytes.Count];
            for (int i = 0; i < bitmapBytes.Count; i++)
            {
                using var stream = new MemoryStream(bitmapBytes[i]);
                bitmaps[i] = new Bitmap(stream);
            }

            SnapshotFrameImages = bitmaps.ToList();
            BlurImageVisible = false;
            NextFrame();
        }
        catch (TaskCanceledException)
        {
            SnapshotRenderProgress = 0;
        }
        catch (Exception e)
        {
            // TODO what to do
            Console.WriteLine(e);
        }
        
        
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