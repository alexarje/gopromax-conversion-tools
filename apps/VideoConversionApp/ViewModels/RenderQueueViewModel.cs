using System.Linq;
using Avalonia.Controls;
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
    private readonly IAppConfigService _appConfigService;
    public SortableObservableCollection<VideoRenderQueueEntry> RenderQueue { get; } = new ();
    
    public IVideoPoolManager VideoPoolManager => _videoPoolManager; 
    public IVideoConverterService ConverterService => _converterService;

    [ObservableProperty] 
    public partial bool ShowExpandedView { get; set; } = true;
    
    [ObservableProperty]
    public partial string NamingPattern { get; set; } = "%o-%c";
    
    
    public RenderQueueViewModel(IVideoConverterService converterService, 
        IVideoPoolManager videoPoolManager,
        IAppConfigService appConfigService)
    {
        if (Design.IsDesignMode)
        {
            _videoPoolManager = new VideoPoolManager(null);
            RenderQueue.Add(new VideoRenderQueueEntry(_videoPoolManager.GetDummyVideo()));
            return;
        }
        
        _converterService = converterService;
        _videoPoolManager = videoPoolManager;
        _appConfigService = appConfigService;

        _videoPoolManager.VideoAddedToPool += VideoPoolManagerOnVideoAddedToPool;
        _videoPoolManager.VideoRemovedFromPool += VideoPoolManagerOnVideoRemovedFromPool;
        
        //_videoPoolManager.GetConversionSettings().OutputFilenamePatternChanged += OnOutputFilenamePatternChanged;
        _appConfigService.GetConfig().PropertyChanged += OnConfigPropertyChanged;
    }

    private void OnConfigPropertyChanged(object? sender, ConfigChangedEventArgs e)
    {
        if (e.PropertyPath == $"{nameof(IAppConfigModel.Conversion)}.{nameof(IConfigConversion.OutputFilenamePattern)}")
            NamingPattern = ((string)e.NewValue)!;
    }

    // private void OnOutputFilenamePatternChanged(object? sender, string pattern)
    // {
    //     NamingPattern = pattern;
    // }

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

    private void VideoOnIsEnabledForConversionUpdated(object? sender, bool enabled)
    {
        var video = (IConvertableVideo) sender!;
        if (enabled && RenderQueue.All(entry => entry.Video != video))
            RenderQueue.Add(new VideoRenderQueueEntry(video));
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
    
}