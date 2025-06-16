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
    private readonly IMediaConverterService _converterService;
    private readonly IConversionManager _conversionManager;
    public SortableObservableCollection<VideoRenderQueueEntry> RenderQueue { get; } = new ();
    
    public IConversionManager ConversionManager => _conversionManager; 

    [ObservableProperty] 
    public partial bool ShowExpandedView { get; set; } = true;
    
    
    public RenderQueueViewModel(IMediaConverterService converterService, IConversionManager conversionManager)
    {
        if (Design.IsDesignMode)
        {
            _conversionManager = new ConversionManager(null);
            RenderQueue.Add(new VideoRenderQueueEntry(_conversionManager.GetDummyVideo()));
            return;
        }
        
        _converterService = converterService;
        _conversionManager = conversionManager;
        
        _conversionManager.VideoAddedToPool += ConversionManagerOnVideoAddedToPool;
        _conversionManager.VideoRemovedFromPool += ConversionManagerOnVideoRemovedFromPool;
    }

    private void ConversionManagerOnVideoRemovedFromPool(object? sender, IConvertableVideo video)
    {
        video.IsEnabledForConversionUpdated -= VideoOnIsEnabledForConversionUpdated;
        var queuedVideo = RenderQueue.FirstOrDefault(entry => entry.Video == video);
        if (queuedVideo != null)
            RenderQueue.Remove(queuedVideo);
    }

    private void ConversionManagerOnVideoAddedToPool(object? sender, IConvertableVideo video)
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
        var selectedForConversion = _conversionManager.ConversionCandidates
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