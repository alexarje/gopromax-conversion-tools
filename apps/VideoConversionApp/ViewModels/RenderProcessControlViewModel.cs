using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoConversionApp.ViewModels;

public partial class RenderProcessControlViewModel : ObservableObject
{
    
    [ObservableProperty]
    public partial bool IsRendering { get; set; }
    [ObservableProperty]
    public partial TimeSpan Elapsed { get; set; }
    [ObservableProperty]
    public partial uint ProcessedCount { get; set; }
    [ObservableProperty]
    public partial uint FailedCount { get; set; }
    [ObservableProperty]
    public partial uint SucceededCount { get; set; }
    [ObservableProperty]
    public partial uint QueueLength { get; set; }
    
    public RenderProcessControlViewModel()
    {
        if (Design.IsDesignMode)
        {
            IsRendering = true;
            Elapsed = TimeSpan.FromSeconds(95);
            ProcessedCount = 1;
            FailedCount = 0;
            SucceededCount = 1;
            QueueLength = 4;
        }
    }
}