using System;
using Avalonia.Controls;
using Avalonia.Input;
using VideoConversionApp.ViewModels;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.Views.Components;

public partial class VideoPlayerView : UserControl
{
    public VideoPlayerView()
    {
        InitializeComponent();
    }
    
    private void OnDataContextChanged(object sender, EventArgs e)
    {
        if (DataContext is VideoPlayerViewModel vm)
        {
            vm.Play();
        }
    }

    private void VideoViewOnPointerEntered(object sender, PointerEventArgs e)
    {
        ControlsPanel.IsVisible = true;
    }

    private void VideoViewOnPointerExited(object sender, PointerEventArgs e)
    {
        ControlsPanel.IsVisible = false;
    }

}