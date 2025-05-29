using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using VideoConversionApp.ViewModels;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.Views;

public partial class MediaSelectionView : UserControl
{
    
    private MediaSelectionViewModel? MediaSelectionViewModel => DataContext as MediaSelectionViewModel;
    public MediaSelectionView()
    {
        InitializeComponent();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MediaSelectionViewModel == null)
            return;
        
        var selectedItem = MediaSelectionListBox.SelectedItem as VideoThumbViewModel;
        MediaSelectionViewModel.MainWindowViewModel.ConversionPreviewViewModel!
            .SetVideoModelAsync(selectedItem?.LinkedConvertibleVideoModel);
    }
}