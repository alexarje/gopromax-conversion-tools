using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using VideoConversionApp.ViewModels.Components;

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
    public partial double KeyFrameVideoRenderProgress { get; set; }
    [ObservableProperty]
    public partial bool BlurImageVisible { get; set; }

    [ObservableProperty]
    public partial double MaximumCropTimelineStartTime { get; set; }

    public double CropTimelineStartTime
    {
        get => field;
        set
        {
            if (value > CropTimelineEndTime)
                value = CropTimelineEndTime;
            SetProperty(ref field, value);
            VideoModel.TimelineCrop.StartTimeMilliseconds = (long)value * 1000;
            VideoModel.NotifyConversionSettingsChanged();
            KeyFrameVideoPlayerViewModel.AssociatedView.CalculateAndSetCropMarkerPositions(VideoModel);
        }
    }
    
    public double CropTimelineEndTime
    {
        get => field;
        set
        {
            if (value < CropTimelineStartTime)
                value = CropTimelineStartTime;
            SetProperty(ref field, value);
            VideoModel.TimelineCrop.EndTimeMilliseconds = (long)value * 1000;
            VideoModel.NotifyConversionSettingsChanged();
            KeyFrameVideoPlayerViewModel.AssociatedView.CalculateAndSetCropMarkerPositions(VideoModel);
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
            VideoModel.FrameRotation.Yaw = value;
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
            VideoModel.NotifyConversionSettingsChanged();
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
            VideoModel.FrameRotation.Pitch = value;
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
            VideoModel.NotifyConversionSettingsChanged();
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
            VideoModel.FrameRotation.Roll = value;
            if (AutoRenderOnChanges)
                _ = LiveUpdateSnapshot();
            VideoModel.NotifyConversionSettingsChanged();
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
            SetProperty(ref field, value);
            if (KeyFrameVideoPlayerViewModel.AssociatedView.IsPanning)
                return;
            var mp = KeyFrameVideoPlayerViewModel.AssociatedView.Player.MediaPlayer;
            mp?.UpdateViewpoint(value, mp.Viewpoint.Pitch, mp.Viewpoint.Roll, mp.Viewpoint.Fov);
        }
    }
    
    public int PreviewPitchValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            SetProperty(ref field, value);
            if (KeyFrameVideoPlayerViewModel.AssociatedView.IsPanning)
                return;
            var mp = KeyFrameVideoPlayerViewModel.AssociatedView.Player.MediaPlayer;
            mp?.UpdateViewpoint(mp.Viewpoint.Yaw, value, mp.Viewpoint.Roll, mp.Viewpoint.Fov);
        }
    }
    
    public int PreviewRollValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            SetProperty(ref field, value);
            if (KeyFrameVideoPlayerViewModel.AssociatedView.IsRolling)
                return;
            var mp = KeyFrameVideoPlayerViewModel.AssociatedView.Player.MediaPlayer;
            mp?.UpdateViewpoint(mp.Viewpoint.Yaw, mp.Viewpoint.Pitch, value, mp.Viewpoint.Fov);
        }
    }
    
    public int PreviewFovValue
    {
        get => field;
        set
        {
            if (VideoModel == null)
                return;
            SetProperty(ref field, value);
            if (KeyFrameVideoPlayerViewModel.AssociatedView.IsFoving)
                return;
            var mp = KeyFrameVideoPlayerViewModel.AssociatedView.Player.MediaPlayer;
            mp?.UpdateViewpoint(mp.Viewpoint.Yaw, mp.Viewpoint.Pitch, mp.Viewpoint.Roll, value);
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
        KeyFrameVideoPlayerViewModel = new VideoPlayerViewModel(null, null);
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
        KeyFrameVideoPlayerViewModel = new VideoPlayerViewModel(videoModel, null);
        
        if (videoModel == null && initialPreviewImage == null)
            return;

        if (initialPreviewImage != null)
        {
            CurrentSnapshotFrameImage = initialPreviewImage;
            SnapshotFrameImages = [initialPreviewImage];
        }
        if (videoModel == null)
            return;

        var prevAutoRenderSetting = AutoRenderOnChanges;
        AutoRenderOnChanges = false;
        TransformPitchValue = videoModel.FrameRotation.Pitch;
        TransformYawValue = videoModel.FrameRotation.Yaw;
        TransformRollValue = videoModel.FrameRotation.Roll;
        AutoRenderOnChanges = prevAutoRenderSetting;

        MaximumCropTimelineStartTime = videoModel.MediaInfo.DurationMilliseconds / 1000.0;
        CropTimelineStartTime = (videoModel.TimelineCrop.StartTimeMilliseconds ?? 0) / 1000.0;
        CropTimelineEndTime = (videoModel.TimelineCrop.EndTimeMilliseconds ?? videoModel.MediaInfo.DurationMilliseconds) / 1000.0;

        ResetPreviewFov();
        ResetPreviewPitch();
        ResetPreviewRoll();
        ResetPreviewYaw();
        
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
        VideoModel.TimelineCrop.StartTimeMilliseconds = null;
        VideoModel.TimelineCrop.EndTimeMilliseconds = null;
        CropTimelineStartTime = 0;
        CropTimelineEndTime = VideoModel.MediaInfo.DurationMilliseconds / 1000.0;
        KeyFrameVideoPlayerViewModel.AssociatedView.CalculateAndSetCropMarkerPositions(VideoModel);
        VideoModel.NotifyConversionSettingsChanged();
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
                KeyFrameVideoPlayerViewModel.PropertyChanged -= OnVideoPlayerOriginatedViewPointPropertyChanged;
            
            KeyFrameVideoPlayerViewModel = new VideoPlayerViewModel(VideoModel, keyFrameVideo);
            KeyFrameVideoPlayerViewModel.PropertyChanged += OnVideoPlayerOriginatedViewPointPropertyChanged;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
    }

    private void OnVideoPlayerOriginatedViewPointPropertyChanged(object? sender, PropertyChangedEventArgs e)
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
        if (e.PropertyName == nameof(VideoPlayerViewModel.CropTimelineStartTime))
            CropTimelineStartTime = KeyFrameVideoPlayerViewModel.CropTimelineStartTime;
        if (e.PropertyName == nameof(VideoPlayerViewModel.CropTimelineEndTime))
            CropTimelineEndTime = KeyFrameVideoPlayerViewModel.CropTimelineEndTime;
    }
}