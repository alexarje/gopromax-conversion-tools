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
        // TODO set crappy initial thumbnail to snapshot frame image (from the listbox item)
        // and show a loading indicator while generating snapshot frames (which is the same
        // as rendering - create a render method and move most of this there...

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
            CurrentSnapshotFrameImage = initialPreviewImage;

        if (videoModel == null)
            return;

        var frameCount = _appSettingsService.GetSettings().NumberOfSnapshotFrames;
        
        // Clear the queue if it's currently generating something already.
        _mediaPreviewService.ClearSnapshotFrameQueue();
        
        //
        // var allTasks = new List<Task>();
        // for (var i = 0; i < frameCount; i++)
        // {
        //     // Never go over max length of the video, and always 1000ms under.
        //     // There seems to be issues generating a frame at the absolute end time.
        //     var timePosition = Math.Min((videoModel.MediaInfo.DurationMilliseconds - 1000), i * skipLength);
        //     var i1 = i;
        //     var task = _mediaPreviewService.QueueSnapshotFrameAsync(videoModel.MediaInfo, timePosition, myCts.Token)
        //         .ContinueWith(task =>
        //         {
        //             if (myCts.Token.IsCancellationRequested)
        //                 return;
        //             
        //             if (task.Result != null)
        //             {
        //                 using var stream = new MemoryStream(task.Result);
        //                 bitmaps[i1] = new Bitmap(stream);
        //             }
        //         });
        //     allTasks.Add(task);
        // }
        //
        // await Task.WhenAll(allTasks);
        
        var bitmapBytes = await _mediaPreviewService.GenerateSnapshotFramesAsync(videoModel.MediaInfo, frameCount, 
            (progress) => SnapshotRenderProgress = progress, myCts.Token);
        
        var bitmaps = new IImage[bitmapBytes.Count];
        for (int i = 0; i < bitmapBytes.Count; i++)
        {
            using var stream = new MemoryStream(bitmapBytes[i]);
            bitmaps[i] = new Bitmap(stream);
        }
        
        SnapshotFrameImages = bitmaps.ToList();
        NextFrame();
        
        
        // OLD BELOW
        //
        // var frames = await _mediaPreviewService.GenerateSnapshotFramesAsync(
        //     videoModel.MediaInfo, frameCount, myCts.Token);
        // var bitmaps = new List<IImage>();
        //
        // // Don't do anything if we were canceled.
        // if (!myCts.Token.IsCancellationRequested)
        // {
        //     foreach (var frame in frames)
        //     {
        //         // Can happen if for some reason ffmpeg fails to generate a frame.
        //         if(frame == null)
        //             continue;
        //     
        //         using var stream = new MemoryStream(frame);
        //         bitmaps.Add(new Bitmap(stream));
        //     }
        //
        //     SnapshotFrameImages = bitmaps;
        //     NextFrame();    
        // }
        //
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