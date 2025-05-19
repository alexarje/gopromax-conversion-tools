using System;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Utils;

namespace VideoConversionApp.ViewModels.Components;

public partial class VideoThumbViewModel : ViewModelBase
{

    [ObservableProperty] public partial string FullFileName { get; set; } = "";
    [ObservableProperty] public partial Bitmap ThumbnailImage { get; set; } = null!;
    [ObservableProperty] public partial string PreviewFileName { get; set; } = "";
    [ObservableProperty] public partial long FileSize { get; set; } = 0;
    [ObservableProperty] public partial double VideoLength { get; set; } = 0;
    [ObservableProperty] public partial DateTime VideoDateTime { get; set; } = DateTime.MinValue;
    [ObservableProperty] public partial bool HasLoadingThumbnail { get; set; } = true;
    [ObservableProperty] public partial bool IsSelectedForConversion { get; set; } = false;

    public string VideoDateTimeString => VideoDateTime.ToString(DataFormattingHelpers.TryResolveActiveCulture());
    public string FileSizeString => FileSize.AsDataQuantityString(2);
    public string VideoLengthString => TimeSpan.FromSeconds(VideoLength).ToString(@"hh\:mm\:ss");

    private static readonly Bitmap DefaultThumbnail = new (AssetLoader.Open(new Uri("avares://VideoConversionApp/Images/videostrip.png")));
    
    public ICommand OnCloseClickCommand { get; set; }
    public IRelayCommand<bool> OnSelectFileCommand { get; set; }
    
    public VideoThumbViewModel()
    {
        if (Design.IsDesignMode)
        {
            ThumbnailImage = DefaultThumbnail;
            //ThumbnailImage = new Bitmap(AssetLoader.Open(new Uri("avares://VideoConversionApp/Images/sample-thn.jpg")));
            FullFileName = "/tmp/GS010146.360";
            PreviewFileName = Path.GetFileName(FullFileName);
            FileSize = 180_556_782;
            VideoDateTime = DateTime.Now;
            VideoLength = 127.5;
            return;
        }

        ThumbnailImage = DefaultThumbnail;
    }
    
    
    
}