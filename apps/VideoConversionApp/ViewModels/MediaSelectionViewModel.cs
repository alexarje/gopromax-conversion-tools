using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.ViewModels;

public partial class MediaSelectionViewModel : MainViewModelPart
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IMediaInfoService _mediaInfoService;
    private readonly IStorageDialogProvider _storageDialogProvider;
    private readonly IMediaPreviewService _mediaPreviewService;
    private readonly IConversionManager _conversionManager;


    // This is a bit ridiculous, how we do not have sort available in ObservableCollection...
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        public void Sort(Comparison<T> comparison)
        {
            ((List<T>)Items).Sort(comparison);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
    
    public bool SortDescending
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            VideoList.Sort(VideoListSortComparison);
        }
    }
    
    public string[] SortOptions =>
    [
        "Date",
        "Filename",
        "Duration"
    ];

    public string SelectedSort
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
            VideoList.Sort(VideoListSortComparison);
        }
    }
    
    [ObservableProperty]
    public partial VideoThumbViewModel? SelectedVideoThumbViewModel { get; set; }

    // [ObservableProperty] // Maybe consider SortedList instead? And notify manually.
    // public partial ObservableCollection<VideoThumbViewModel> VideoList { get; private set; } =
    //     new ObservableCollection<VideoThumbViewModel>();
    public SortableObservableCollection<VideoThumbViewModel> VideoList
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
        }
    } = new SortableObservableCollection<VideoThumbViewModel>();
    
    public MediaSelectionViewModel(IServiceProvider serviceProvider, 
        IAppSettingsService appSettingsService,
        IMediaInfoService mediaInfoService, 
        IStorageDialogProvider storageDialogProvider,
        IMediaPreviewService mediaPreviewService,
        IConversionManager conversionManager) : base(serviceProvider)
    {
        _appSettingsService = appSettingsService;
        _mediaInfoService = mediaInfoService;
        _storageDialogProvider = storageDialogProvider;
        _mediaPreviewService = mediaPreviewService;
        _conversionManager = conversionManager;
        SelectedSort = SortOptions.First();

        if (Design.IsDesignMode)
        {
            VideoList.Add(new VideoThumbViewModel());
            VideoList.Add(new VideoThumbViewModel());
        }
        
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedVideoThumbViewModel))
        {
            _ = MainWindowViewModel.ConversionPreviewViewModel!.SetActiveVideoModelAsync(
                SelectedVideoThumbViewModel?.LinkedConvertibleVideoModel, SelectedVideoThumbViewModel?.ThumbnailImage);
        }
    }


    [RelayCommand]
    private async Task AddFiles()
    {
        var appSettings = _appSettingsService.GetSettings();
        var videoType = new FilePickerFileType("GoPro MAX .360");
        videoType.Patterns = [".360"];

        var selectedFiles = await _storageDialogProvider!.GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = true,
            FileTypeFilter = [videoType]
        });

        var thumbGenerationJobs = new List<(VideoThumbViewModel thumbViewModel, MediaInfo mediaInfo)>();
        foreach (var selectedFile in selectedFiles)
        {
            var fullFilename = selectedFile!.TryGetLocalPath();
            if (VideoList.Any(v => v.FullFileName == fullFilename))
                continue;
            
            var mediaInfo = await _mediaInfoService.GetMediaInfoAsync(fullFilename!);
            var convertibleVideo = new ConvertibleVideoModel(mediaInfo);
            convertibleVideo.OnConversionSettingsChanged += ConvertibleVideoOnOnConversionSettingsChanged;
            if (mediaInfo.IsValidVideo && mediaInfo.IsGoProMaxFormat)
                _conversionManager.AddToConversionCandidates(convertibleVideo);

            var thumbViewModel = new VideoThumbViewModel
            {
                FullFileName = fullFilename!,
                PreviewFileName = Path.GetFileName(fullFilename!),
                FileSize = mediaInfo.SizeBytes,
                VideoDateTime = mediaInfo.CreatedDateTime,
                VideoLengthSeconds = (double)mediaInfo.DurationInSeconds,
                ShowAsSelectedForConversion = mediaInfo.IsGoProMaxFormat && convertibleVideo.IsEnabledForConversion,
                LinkedConvertibleVideoModel = mediaInfo.IsGoProMaxFormat ? convertibleVideo : null,
                HasProblems = !mediaInfo.IsGoProMaxFormat || !mediaInfo.IsValidVideo,
                ToolTipMessage = mediaInfo.ValidationIssues is { Length: > 0 } 
                    ? string.Join("\n", new[] {"Video has issues, cannot use:"}.Concat(mediaInfo.ValidationIssues)) 
                    : null!
            };

            thumbViewModel.OnCloseClickCommand = new RelayCommand(() =>
            {
                VideoList.Remove(thumbViewModel);
                _conversionManager.RemoveFromConversionCandidates(thumbViewModel.LinkedConvertibleVideoModel);
                if (thumbViewModel.LinkedConvertibleVideoModel != null)
                    thumbViewModel.LinkedConvertibleVideoModel.OnConversionSettingsChanged -= ConvertibleVideoOnOnConversionSettingsChanged;
            });
            
            thumbViewModel.OnSelectFileCommand = new RelayCommand<bool>((isChecked) =>
            {
                thumbViewModel.LinkedConvertibleVideoModel.IsEnabledForConversion = isChecked;
                thumbViewModel.ShowAsSelectedForConversion = isChecked;
            });
            thumbGenerationJobs.Add((thumbViewModel, mediaInfo));
            VideoList.Add(thumbViewModel);
        }
        
        VideoList.Sort(VideoListSortComparison);

        
        for (var i = 0; i < thumbGenerationJobs.Count; i++)
        {
            var item = thumbGenerationJobs[i];
            var i1 = i;
            var thumbTimePositionMs = appSettings.ThumbnailAtPosition / 100.0 * (long)(item.mediaInfo.DurationInSeconds * 1000);
            _ = _mediaPreviewService.QueueThumbnailGenerationAsync(item.mediaInfo, (long)thumbTimePositionMs)
                .ContinueWith(task =>
                {
                    if (task.Result != null)
                    {
                        using var stream = new MemoryStream(task.Result);
                        thumbGenerationJobs[i1].thumbViewModel.ThumbnailImage = new Bitmap(stream);
                        thumbGenerationJobs[i1].thumbViewModel.HasLoadingThumbnail = false;
                    }
                });
        }

    }

    private int VideoListSortComparison(VideoThumbViewModel x, VideoThumbViewModel y)
    {
        if (SelectedSort == "Date")
        {
            if (SortDescending)
                return x.VideoDateTime > y.VideoDateTime ? -1 : 1;
            else
                return x.VideoDateTime < y.VideoDateTime ? -1 : 1;
        }

        if (SelectedSort == "Filename")
        {
            if (SortDescending)
                return String.CompareOrdinal(y.PreviewFileName, x.PreviewFileName);
            else
                return String.CompareOrdinal(x.PreviewFileName, y.PreviewFileName);
        }

        if (SelectedSort == "Duration")
        {
            if (SortDescending)
                return x.VideoLengthSeconds > y.VideoLengthSeconds ? -1 : 1;
            else
                return x.VideoLengthSeconds < y.VideoLengthSeconds ? -1 : 1;
        }

        return 0;
    }

    private void ConvertibleVideoOnOnConversionSettingsChanged(object? sender, bool e)
    {
        var convertibleVideo = sender as ConvertibleVideoModel;
        var settingsChanged = e;

        var videoThumbViewModel = VideoList.FirstOrDefault(x => x.LinkedConvertibleVideoModel == convertibleVideo);
        if (videoThumbViewModel == null)
            return;
        
        videoThumbViewModel.HasConversionSettingsModified = settingsChanged;
    }

    [RelayCommand]
    private void ClearAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            if (videoThumbViewModel.LinkedConvertibleVideoModel != null)
            {
                _conversionManager.RemoveFromConversionCandidates(videoThumbViewModel.LinkedConvertibleVideoModel);
                videoThumbViewModel.LinkedConvertibleVideoModel.OnConversionSettingsChanged -= ConvertibleVideoOnOnConversionSettingsChanged;
            }
        }
        VideoList.Clear();
    }

    [RelayCommand]
    private void SelectAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            if (videoThumbViewModel.LinkedConvertibleVideoModel == null) 
                continue;
            
            videoThumbViewModel.LinkedConvertibleVideoModel.IsEnabledForConversion = true;
            videoThumbViewModel.ShowAsSelectedForConversion = true;
        }
    }

    [RelayCommand]
    private void UnselectAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            if (videoThumbViewModel.LinkedConvertibleVideoModel == null) 
                continue;
            
            videoThumbViewModel.LinkedConvertibleVideoModel.IsEnabledForConversion = false;
            videoThumbViewModel.ShowAsSelectedForConversion = false;
        }
    }
    
    
}