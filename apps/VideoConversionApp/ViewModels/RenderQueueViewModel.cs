using VideoConversionApp.Utils;

namespace VideoConversionApp.ViewModels;

public class RenderQueueViewModel : ViewModelBase
{
    public SortableObservableCollection<object> RenderQueue { get; } = new SortableObservableCollection<object>();

    public RenderQueueViewModel()
    {
        RenderQueue.Add("item");
    }
    
}