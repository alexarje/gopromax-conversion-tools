using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Services;
using VideoConversionApp.Utils;

namespace VideoConversionApp.ViewModels;

public partial class RenderQueueViewModel : ViewModelBase
{
    private readonly IMediaConverterService _converterService;
    private readonly IConversionManager _conversionManager;
    public SortableObservableCollection<IConvertableVideo> RenderQueue { get; } = new ();
    
    public IConversionManager ConversionManager => _conversionManager; 

    [ObservableProperty] 
    public partial bool ShowExpandedView { get; set; } = true;
    
    
    public RenderQueueViewModel(IMediaConverterService converterService, IConversionManager conversionManager)
    {
        if (Design.IsDesignMode)
        {
            _conversionManager = new ConversionManager(null);
            RenderQueue.Add(_conversionManager.GetDummyVideo());
            return;
        }
        
        _converterService = converterService;
        _conversionManager = conversionManager;

        
    }
 
    // TODO Observe/fill render queue with selected videos 
    
    
}