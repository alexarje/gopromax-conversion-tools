using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using VideoConversionApp.Services;
using VideoConversionApp.Utils;

namespace VideoConversionApp.ViewModels;

public partial class RenderQueueViewModel : ViewModelBase
{
    private readonly IVideoConverterService _converterService;
    private readonly IVideoPoolManager _videoPoolManager;
    private readonly IBitmapCache _bitmapCache;
    private readonly IAppConfigService _appConfigService;
    public SortableObservableCollection<VideoRenderQueueEntry> RenderQueue { get; } = new ();
    
    public IVideoPoolManager VideoPoolManager => _videoPoolManager; 
    public IVideoConverterService ConverterService => _converterService;

    [ObservableProperty]
    public partial bool IsRenderingInProgress { get; set; } // TODO have events in IVideoConverterService and update this with them
    
    [ObservableProperty] 
    public partial bool ShowExpandedView { get; set; } = true;
    
    [ObservableProperty]
    public partial string NamingPattern { get; set; } = "%o-%c";
    
    [ObservableProperty]
    public partial string OutputDirectory { get; set; } = "";
    
    [ObservableProperty]
    public partial bool OutputBesideOriginals { get; set; }
    
    
    public RenderQueueViewModel(IVideoConverterService converterService, 
        IVideoPoolManager videoPoolManager,
        IBitmapCache bitmapCache,
        IAppConfigService appConfigService)
    {
        if (Design.IsDesignMode)
        {
            _videoPoolManager = new VideoPoolManager(null);
            var newEntry = new VideoRenderQueueEntry(_videoPoolManager.GetDummyVideo());
            newEntry.Thumbnail = GetThumbForDesigner();
            newEntry.RenderingState = VideoRenderingState.Queued;
            RenderQueue.Add(newEntry);
            return;
        }
        
        _converterService = converterService;
        _videoPoolManager = videoPoolManager;
        _bitmapCache = bitmapCache;
        _appConfigService = appConfigService;

        var config = appConfigService.GetConfig();
        NamingPattern = config.Conversion.OutputFilenamePattern;
        OutputDirectory = config.Conversion.OutputDirectory;
        OutputBesideOriginals = config.Conversion.OutputBesideOriginals;

        _videoPoolManager.VideoAddedToPool += VideoPoolManagerOnVideoAddedToPool;
        _videoPoolManager.VideoRemovedFromPool += VideoPoolManagerOnVideoRemovedFromPool;
        
        _appConfigService.GetConfig().PropertyChanged += OnConfigPropertyChanged;
    }

    private void OnConfigPropertyChanged(object? sender, ConfigChangedEventArgs e)
    {
        if (e.PropertyPath.StartsWith(nameof(IAppConfigModel.Conversion)))
        {
            if (e.PropertyPath.EndsWith($".{nameof(IConfigConversion.OutputFilenamePattern)}"))
            {
                NamingPattern = (string)e.NewValue!;
            }
            if (e.PropertyPath.EndsWith($".{nameof(IConfigConversion.OutputDirectory)}"))
            {
                OutputDirectory = (string)e.NewValue!;
            }
            if (e.PropertyPath.EndsWith($".{nameof(IConfigConversion.OutputBesideOriginals)}"))
            {
                OutputBesideOriginals = (bool)e.NewValue!;
            }
        }
        
    }
    

    private void VideoPoolManagerOnVideoRemovedFromPool(object? sender, IConvertableVideo video)
    {
        video.IsEnabledForConversionUpdated -= VideoOnIsEnabledForConversionUpdated;
        var queuedVideo = RenderQueue.FirstOrDefault(entry => entry.Video == video);
        if (queuedVideo != null)
            RenderQueue.Remove(queuedVideo);
    }

    private void VideoPoolManagerOnVideoAddedToPool(object? sender, IConvertableVideo video)
    {
        video.IsEnabledForConversionUpdated -= VideoOnIsEnabledForConversionUpdated;
        video.IsEnabledForConversionUpdated += VideoOnIsEnabledForConversionUpdated;
    }

    private Bitmap GetThumbForDesigner()
    {
        using var defaultThumb = AssetLoader.Open(new Uri("avares://VideoConversionApp/Images/sample-thn-256.jpg"));
        return new Bitmap(defaultThumb);
    }

    private void VideoOnIsEnabledForConversionUpdated(object? sender, bool enabled)
    {
        var video = (IConvertableVideo) sender!;
        if (enabled && RenderQueue.All(entry => entry.Video != video))
        {
            var newEntry = new VideoRenderQueueEntry(video)
            {
                Thumbnail = _bitmapCache.Get(video.InputVideoInfo.Filename)
            };
            if (Design.IsDesignMode)
                newEntry.Thumbnail = GetThumbForDesigner();

            RenderQueue.Add(newEntry);
        }
        if (!enabled)
        {
            var queuedVideo = RenderQueue.FirstOrDefault(entry => entry.Video == video);
            if (queuedVideo != null)
                RenderQueue.Remove(queuedVideo);
        }
    }

    public void SyncRenderQueue()
    {
        var selectedForConversion = _videoPoolManager.VideoPool
            .Where(video => video.IsEnabledForConversion)
            .ToList();
        
        selectedForConversion.ToList().ForEach(video =>
        {
            if (RenderQueue.All(renderQueueEntry => renderQueueEntry.Video != video))
                RenderQueue.Add(new VideoRenderQueueEntry(video));
        });

        var removed = RenderQueue.Where(entry => !selectedForConversion.Contains(entry.Video)).ToList();
        removed.ForEach(entry => RenderQueue.Remove(entry));

    }

    [RelayCommand]
    public void ClearRenderQueue()
    {
        var items = RenderQueue.ToList();
        // Event handler handles the rest.
        items.ForEach(item => item.Video.IsEnabledForConversion = false);
    }

    [RelayCommand]
    public void ClearCompletedFromQueue()
    {
        var items = RenderQueue
            .Where(x => x.RenderingState == VideoRenderingState.CompletedSuccessfully).ToList();
        items.ForEach(item => item.Video.IsEnabledForConversion = false);
    }

    [RelayCommand]
    public void MoveUpQueueEntry(VideoRenderQueueEntry entry)
    {
        
    }
    
    [RelayCommand]
    public void MoveDownQueueEntry(VideoRenderQueueEntry entry)
    {
        
    }
    
    [RelayCommand]
    public void RemoveQueueEntry(VideoRenderQueueEntry entry)
    {
        
    }
    
    [RelayCommand]
    public void CancelRenderingQueueEntry(VideoRenderQueueEntry entry)
    {
        
    }
    
    [RelayCommand]
    public void RetryRenderingQueueEntry(VideoRenderQueueEntry entry)
    {
        
    }
}