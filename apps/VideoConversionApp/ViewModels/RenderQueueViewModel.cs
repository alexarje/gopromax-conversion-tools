using CommunityToolkit.Mvvm.ComponentModel;
using VideoConversionApp.Utils;

namespace VideoConversionApp.ViewModels;

public partial class RenderQueueViewModel : ViewModelBase
{
    public SortableObservableCollection<object> RenderQueue { get; } = new SortableObservableCollection<object>();

    [ObservableProperty] public partial bool MinimalView { get; set; } = false;
    
    public RenderQueueViewModel()
    {
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
        RenderQueue.Add("item");
    }
    
}