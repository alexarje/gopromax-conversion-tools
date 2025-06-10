using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using VideoConversionApp.Utils;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.ViewModels;

public partial class ConversionPreviewViewModel : ViewModelBase
{
    private readonly IMediaPreviewService _mediaPreviewService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly IConversionManager _conversionManager;

    public IList<IImage> SnapshotFrameImages { get; set; } = [];

    [ObservableProperty] 
    public partial IConvertibleVideoModel VideoModel { get; set; } = null!;
    [ObservableProperty]
    public partial IImage? CurrentSnapshotFrameImage { get; set; }
    [ObservableProperty]
    public partial double SnapshotRenderProgress { get; set; }
    [ObservableProperty]
    public partial double KeyFrameVideoRenderProgress { get; set; }
    [ObservableProperty]
    public partial bool BlurImageVisible { get; set; }

    [ObservableProperty]
    public partial decimal MaximumCropTimelineTime { get; set; }

    public decimal CropTimelineStartTime
    {
        get => field;
        set
        {
            if (value == field)
                return;
            
            if (value > CropTimelineEndTime)
                value = CropTimelineEndTime;
            
            SetProperty(ref field, value);
            VideoModel.TimelineCrop = VideoModel.TimelineCrop with { StartTimeSeconds = value };
            //VideoModel.NotifyConversionSettingsChanged();
            //KeyFrameVideoPlayerViewModel.AssociatedView.CalculateAndSetCropMarkerPositions(VideoModel);
        }
    }
    
    public decimal CropTimelineEndTime
    {
        get => field;
        set
        {
            if (value == field)
                return;
            
            if (value < CropTimelineStartTime)
                value = CropTimelineStartTime;
            
            SetProperty(ref field, value);
            VideoModel.TimelineCrop = VideoModel.TimelineCrop with { EndTimeSeconds = value };
            //VideoModel.NotifyConversionSettingsChanged();
            //KeyFrameVideoPlayerViewModel.AssociatedView.CalculateAndSetCropMarkerPositions(VideoModel);
        }
    }
    
    [ObservableProperty]
    public partial VideoPlayerViewModel KeyFrameVideoPlayerViewModel { get; set; }

    public bool AutoRenderOnChanges
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            if (value)
                _ = LiveUpdateSnapshot();
        }
    }
    
    
    public int TransformYawValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            SetProperty(ref field, value);
            VideoModel.FrameRotation = VideoModel.FrameRotation with { Yaw = value };
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
            //VideoModel.NotifyConversionSettingsChanged();
        }
    }
    
    public int TransformPitchValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            SetProperty(ref field, value);
            VideoModel.FrameRotation = VideoModel.FrameRotation with { Pitch = value };
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
            //VideoModel.NotifyConversionSettingsChanged();
        }
    }
    
    public int TransformRollValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            SetProperty(ref field, value);
            VideoModel.FrameRotation = VideoModel.FrameRotation with { Roll = value };
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
            //VideoModel.NotifyConversionSettingsChanged();
        }
    }
    
    // For KeyFrameVideo Preview
    
    public int PreviewYawValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            
            if (value == field)
                return;
            
            SetProperty(ref field, value);
            if (!KeyFrameVideoPlayerViewModel.IsDragging)
                KeyFrameVideoPlayerViewModel.VideoPlayerYaw = value;
        }
    }
    
    public int PreviewPitchValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            
            if (value == field)
                return;
            
            SetProperty(ref field, value);
            if (!KeyFrameVideoPlayerViewModel.IsDragging)
                KeyFrameVideoPlayerViewModel.VideoPlayerPitch = value;
        }
    }
    
    public int PreviewRollValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            
            if (value == field)
                return;
            
            SetProperty(ref field, value);
            if (!KeyFrameVideoPlayerViewModel.IsDragging)
                KeyFrameVideoPlayerViewModel.VideoPlayerRoll = value;
        }
    }
    
    public int PreviewFovValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            
            if (value == field)
                return;
            
            SetProperty(ref field, value);
            if (!KeyFrameVideoPlayerViewModel.IsDragging)
                KeyFrameVideoPlayerViewModel.VideoPlayerFov = value;
            
            // if (KeyFrameVideoPlayerViewModel.AssociatedView.IsFoving)
            //     return;
            // var mp = KeyFrameVideoPlayerViewModel.AssociatedView.Player.MediaPlayer;
            // mp?.UpdateViewpoint(mp.Viewpoint.Yaw, mp.Viewpoint.Pitch, mp.Viewpoint.Roll, value);
        }
    }

    private CancellationTokenSource? _snapshotGenerationCts;
    
    private CancellationTokenSource? _snapshotLiveUpdateCts;
    
    public ConversionPreviewViewModel(
        IMediaPreviewService mediaPreviewService,
        IAppSettingsService appSettingsService,
        IConversionManager conversionManager)
    {
        _mediaPreviewService = mediaPreviewService;
        _appSettingsService = appSettingsService;
        _conversionManager = conversionManager;
        _conversionManager.PreviewedVideoChanged += ConversionManagerOnPreviewedVideoChanged;
        KeyFrameVideoPlayerViewModel = new VideoPlayerViewModel(null, null);
    }

    private void ConversionManagerOnPreviewedVideoChanged(object? sender, IConvertibleVideoModel? video)
    {
        if (video == null)
            return;
        
        IImage? thumb = null;
        var thumbBytes = _mediaPreviewService.GetCachedThumbnail(video.MediaInfo);
        if (thumbBytes != null)
            thumb = thumbBytes.ToBitmap();

        _ = SetActiveVideoModelAsync(video, thumb);
    }

    /// <summary>
    /// Sets the active video in the main preview pane.
    /// Triggers the generation of snapshot frames.
    /// </summary>
    /// <param name="video"></param>
    /// <param name="initialPreviewImage">Image to show initially in the snapshot image control before
    /// the snapshot frames get generated.</param>
    public async Task SetActiveVideoModelAsync(IConvertibleVideoModel? video, IImage? initialPreviewImage)
    {
        if (video == VideoModel)
            return;
        
        BlurImageVisible = true;
        VideoModel = video;
        KeyFrameVideoPlayerViewModel = new VideoPlayerViewModel(video, null);
        
        if (video == null && initialPreviewImage == null)
            return;

        if (initialPreviewImage != null)
        {
            CurrentSnapshotFrameImage = initialPreviewImage;
            SnapshotFrameImages = [initialPreviewImage];
        }
        if (video == null)
            return;

        video.TimelineCropUpdated += OnVideoTimelineCropUpdated;
        
        var prevAutoRenderSetting = AutoRenderOnChanges;
        AutoRenderOnChanges = false;
        TransformPitchValue = video.FrameRotation.Pitch;
        TransformYawValue = video.FrameRotation.Yaw;
        TransformRollValue = video.FrameRotation.Roll;
        AutoRenderOnChanges = prevAutoRenderSetting;

        MaximumCropTimelineTime = video.MediaInfo.DurationInSeconds;
        CropTimelineStartTime = video.TimelineCrop.StartTimeSeconds ?? 0;
        CropTimelineEndTime = video.TimelineCrop.EndTimeSeconds ?? video.MediaInfo.DurationInSeconds;

        ResetPreviewFov();
        ResetPreviewPitch();
        ResetPreviewRoll();
        ResetPreviewYaw();
        
        await RenderSnapshotFramesAsync();
        NextFrame();
    }

    private void OnVideoTimelineCropUpdated(object? sender, TimelineCrop e)
    {
        var video = sender as IConvertibleVideoModel;
        CropTimelineStartTime = e.StartTimeSeconds ?? 0;
        CropTimelineEndTime = e.EndTimeSeconds ?? video!.MediaInfo.DurationInSeconds;
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
                bitmaps[i] = bitmapBytes[i].ToBitmap();

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

    [RelayCommand]
    public void ResetTransformYaw()
    {
        TransformYawValue = 0;
    }
    
    [RelayCommand]
    public void ResetTransformPitch()
    {
        TransformPitchValue = 0;
    }
    
    [RelayCommand]
    public void ResetTransformRoll()
    {
        TransformRollValue = 0;
    }
    
    [RelayCommand]
    public void ResetPreviewYaw()
    {
        PreviewYawValue = 0;
    }
    
    [RelayCommand]
    public void ResetPreviewPitch()
    {
        PreviewPitchValue = 0;
    }
    
    [RelayCommand]
    public void ResetPreviewRoll()
    {
        PreviewRollValue = 0;
    }
    
    [RelayCommand]
    public void ResetPreviewFov()
    {
        PreviewFovValue = 80;
    }

    [RelayCommand]
    public void ResetVideoTimelineCrop()
    {
        if (VideoModel == null)
            return;

        VideoModel.TimelineCrop = new TimelineCrop();
        CropTimelineStartTime = 0;
        CropTimelineEndTime = VideoModel.MediaInfo.DurationInSeconds;
       
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

    [RelayCommand]
    public async Task RenderKeyFrameVideoAsync()
    {
        try
        {
            var keyFrameVideo = await _mediaPreviewService.GenerateKeyFrameVideoAsync(VideoModel,
                (progress) => KeyFrameVideoRenderProgress = progress, CancellationToken.None);

            // Set the UserControl's bound ViewModel. Listen to its PropertyChanged events to sync the yaw/pitch/roll/fov
            // from there to our properties.
            if (KeyFrameVideoPlayerViewModel != null)
                KeyFrameVideoPlayerViewModel.PropertyChanged -= OnVideoPlayerViewModelPropertyChanged;
            
            KeyFrameVideoPlayerViewModel = new VideoPlayerViewModel(VideoModel, keyFrameVideo);
            KeyFrameVideoPlayerViewModel.PropertyChanged += OnVideoPlayerViewModelPropertyChanged;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
    }

    private void OnVideoPlayerViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VideoPlayerViewModel.VideoPlayerYaw)) 
            PreviewYawValue = (int)KeyFrameVideoPlayerViewModel.VideoPlayerYaw;
        if (e.PropertyName == nameof(VideoPlayerViewModel.VideoPlayerPitch)) 
            PreviewPitchValue = (int)KeyFrameVideoPlayerViewModel.VideoPlayerPitch;
        if (e.PropertyName == nameof(VideoPlayerViewModel.VideoPlayerRoll)) 
            PreviewRollValue = (int)KeyFrameVideoPlayerViewModel.VideoPlayerRoll;
        if (e.PropertyName == nameof(VideoPlayerViewModel.VideoPlayerFov))
            PreviewFovValue = (int)KeyFrameVideoPlayerViewModel.VideoPlayerFov;
        
        // Yeah, there will be some double updates going on, and this whole mess of a video control must be
        // implemented better at some point...
        // if (e.PropertyName == nameof(VideoPlayerViewModel.CropTimelineStartTime))
        //     CropTimelineStartTime = KeyFrameVideoPlayerViewModel.CropTimelineStartTime;
        // if (e.PropertyName == nameof(VideoPlayerViewModel.CropTimelineEndTime))
        //     CropTimelineEndTime = KeyFrameVideoPlayerViewModel.CropTimelineEndTime;
        
        
    }
}