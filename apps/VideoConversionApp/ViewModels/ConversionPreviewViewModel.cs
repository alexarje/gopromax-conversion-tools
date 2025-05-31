using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
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

    public IList<IImage> SnapshotFrameImages { get; set; } = [];

    [ObservableProperty] 
    public partial ConvertibleVideoModel VideoModel { get; set; } = null!;
    [ObservableProperty]
    public partial IImage? CurrentSnapshotFrameImage { get; set; }
    [ObservableProperty]
    public partial double SnapshotRenderProgress { get; set; }
    [ObservableProperty]
    public partial bool BlurImageVisible { get; set; }
    [ObservableProperty]
    public partial bool AutoRenderOnChanges { get; set; }

    public int TransformYawValue
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            VideoModel.FrameRotation.Yaw = value;
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
        }
    }
    
    public int TransformPitchValue
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            VideoModel.FrameRotation.Pitch = value;
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
        }
    }
    
    public int TransformRollValue
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            VideoModel.FrameRotation.Roll = value;
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
        }
    }

    private CancellationTokenSource? _snapshotGenerationCts;
    
    private CancellationTokenSource? _snapshotLiveUpdateCts;
    
    public ConversionPreviewViewModel(IServiceProvider serviceProvider,
        IMediaPreviewService mediaPreviewService,
        IAppSettingsService appSettingsService) : base(serviceProvider)
    {
        _mediaPreviewService = mediaPreviewService;
        _appSettingsService = appSettingsService;
    }

    /// <summary>
    /// Sets the active video in the main preview pane.
    /// Triggers the generation of snapshot frames.
    /// </summary>
    /// <param name="videoModel"></param>
    /// <param name="initialPreviewImage">Image to show initially in the snapshot image control before
    /// the snapshot frames get generated.</param>
    public async Task SetActiveVideoModelAsync(ConvertibleVideoModel? videoModel, IImage? initialPreviewImage)
    {
        BlurImageVisible = true;
        VideoModel = videoModel;
        
        if (videoModel == null && initialPreviewImage == null)
            return;

        if (initialPreviewImage != null)
        {
            CurrentSnapshotFrameImage = initialPreviewImage;
            SnapshotFrameImages = [initialPreviewImage];
        }
        if (videoModel == null)
            return;

        await RenderSnapshotFramesAsync();
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

    /// <summary>
    /// Renders/generates the snapshot frames from the currently active video.
    /// Removes the blur image upon success.
    /// </summary>
    public async Task RenderSnapshotFramesAsync()
    {
        // If there's already this operation in progress, cancel the previous one.
        if (_snapshotGenerationCts != null && !_snapshotGenerationCts.Token.IsCancellationRequested)
            _snapshotGenerationCts.Cancel();

        var myCts = new CancellationTokenSource();
        _snapshotGenerationCts = myCts;
        
        var frameCount = _appSettingsService.GetSettings().NumberOfSnapshotFrames;
        
        
        try
        {
            var transformationSettings = new SnapshotFrameTransformationSettings()
            {
                Rotation = VideoModel.FrameRotation
            };
            var bitmapBytes = await _mediaPreviewService.GenerateSnapshotFramesAsync(VideoModel.MediaInfo,
                transformationSettings, frameCount, (progress) => SnapshotRenderProgress = progress, myCts.Token);

            var bitmaps = new IImage[bitmapBytes.Count];
            for (int i = 0; i < bitmapBytes.Count; i++)
            {
                using var stream = new MemoryStream(bitmapBytes[i]);
                bitmaps[i] = new Bitmap(stream);
            }

            SnapshotFrameImages = bitmaps.ToList();
            BlurImageVisible = false;
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
    public async Task OnRenderSnapshotFramesAsync()
    {
        var frameIndex = SnapshotFrameImages.IndexOf(CurrentSnapshotFrameImage);
        await RenderSnapshotFramesAsync();
        if (frameIndex != -1 && frameIndex <= SnapshotFrameImages.Count)
            CurrentSnapshotFrameImage = SnapshotFrameImages[frameIndex];
        else
            NextFrame();
    }

    private async Task LiveUpdateSnapshot()
    {
        if (_snapshotLiveUpdateCts != null && !_snapshotLiveUpdateCts.Token.IsCancellationRequested)
            _snapshotLiveUpdateCts.Cancel();

        _snapshotLiveUpdateCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(250, _snapshotLiveUpdateCts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await OnRenderSnapshotFramesAsync();

    }
}