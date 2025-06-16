using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using VideoConversionApp.Utils;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.ViewModels;

public partial class MediaSelectionViewModel : ViewModelBase
{
    private readonly IAppSettingsService _appSettingsService;
    private readonly IMediaInfoService _mediaInfoService;
    private readonly IStorageDialogProvider _storageDialogProvider;
    private readonly IMediaPreviewService _mediaPreviewService;
    private readonly IConversionManager _conversionManager;
    private readonly ConversionPreviewViewModel _conversionPreviewViewModel;

    [ObservableProperty]
    public partial bool SortDescending { get; set; }
    
    public string[] SortOptions =>
    [
        "Date",
        "Filename",
        "Duration"
    ];
    [ObservableProperty]
    public partial string SelectedSort { get; set; }
    
    [ObservableProperty]
    public partial VideoThumbViewModel? SelectedVideoThumbViewModel { get; set; }
    
    public SortableObservableCollection<VideoThumbViewModel> VideoList
    {
        get => field;
        set
        {
            SetProperty(ref field, value);
        }
    } = new SortableObservableCollection<VideoThumbViewModel>();
    
    public MediaSelectionViewModel(
        IAppSettingsService appSettingsService,
        IMediaInfoService mediaInfoService, 
        IStorageDialogProvider storageDialogProvider,
        IMediaPreviewService mediaPreviewService,
        IConversionManager conversionManager,
        ConversionPreviewViewModel conversionPreviewViewModel)
    {
        _appSettingsService = appSettingsService;
        _mediaInfoService = mediaInfoService;
        _storageDialogProvider = storageDialogProvider;
        _mediaPreviewService = mediaPreviewService;
        _conversionManager = conversionManager;
        _conversionPreviewViewModel = conversionPreviewViewModel;
        SelectedSort = SortOptions.First();

        if (Design.IsDesignMode)
        {
            VideoList.Add(new VideoThumbViewModel());
            VideoList.Add(new VideoThumbViewModel());
            return;
        }
        
        _conversionManager.VideoRemovedFromPool += ConversionManagerOnVideoRemovedFromPool;
        
    }

    private void ConversionManagerOnVideoRemovedFromPool(object? sender, IConvertableVideo video)
    {
        var match = VideoList.FirstOrDefault(x => x.LinkedVideo == video);
        if (match != null)
            VideoList.Remove(match);
    }

    partial void OnSortDescendingChanged(bool value)
    {
        VideoList.Sort(VideoListSortComparison);
    }

    partial void OnSelectedSortChanged(string value)
    {
        VideoList.Sort(VideoListSortComparison);
    }

    partial void OnSelectedVideoThumbViewModelChanged(VideoThumbViewModel? value)
    {
        _ = _conversionPreviewViewModel.SetPreviewedVideo(value?.LinkedVideo);
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

        var thumbGenerationJobs = new List<(VideoThumbViewModel thumbViewModel, IMediaInfo mediaInfo)>();
        foreach (var selectedFile in selectedFiles)
        {
            var fullFilename = selectedFile!.TryGetLocalPath();
            if (VideoList.Any(v => v.FullFileName == fullFilename))
                continue;
            
            var mediaInfo = await _mediaInfoService.ParseMediaAsync(fullFilename!);
            IConvertableVideo? video = null;
            if (mediaInfo.IsValidVideo && mediaInfo.IsGoProMaxFormat)
            {
                video = _conversionManager.AddVideoToPool(mediaInfo);
                video.SettingsChanged += VideoOnConversionSettingsChanged;
                video.IsEnabledForConversionUpdated += VideoOnIsEnabledForConversionUpdated;
            }

            var thumbViewModel = new VideoThumbViewModel
            {
                FullFileName = fullFilename!,
                PreviewFileName = Path.GetFileName(fullFilename!),
                FileSize = mediaInfo.SizeBytes,
                VideoDateTime = mediaInfo.CreatedDateTime,
                VideoLengthSeconds = (double)mediaInfo.DurationInSeconds,
                ShowAsSelectedForConversion = false,
                LinkedVideo = video,
                HasProblems = !mediaInfo.IsGoProMaxFormat || !mediaInfo.IsValidVideo,
                ToolTipMessage = mediaInfo.ValidationIssues is { Length: > 0 } 
                    ? string.Join("\n", new[] {"Video has issues, cannot use:"}.Concat(mediaInfo.ValidationIssues)) 
                    : null!
            };

            thumbViewModel.OnCloseClickCommand = new RelayCommand(() =>
            {
                VideoList.Remove(thumbViewModel);
                if (thumbViewModel.LinkedVideo != null)
                {
                    _conversionManager.RemoveVideoFromPool(thumbViewModel.LinkedVideo);
                }
            });
            
            thumbViewModel.OnVideoCheckedChangedCommand = new RelayCommand<bool>((isChecked) =>
            {
                if (thumbViewModel.LinkedVideo != null)
                    thumbViewModel.LinkedVideo.IsEnabledForConversion = isChecked;
            });
            thumbGenerationJobs.Add((thumbViewModel, mediaInfo));
            VideoList.Add(thumbViewModel);
        }
        
        VideoList.Sort(VideoListSortComparison);

        for (var i = 0; i < thumbGenerationJobs.Count; i++)
        {
            var item = thumbGenerationJobs[i];
            var i1 = i;
            var thumbTimePositionMs = appSettings.Previews.ThumbnailTimePositionPcnt / 100.0 * (long)(item.mediaInfo.DurationInSeconds * 1000);
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

    private void VideoOnIsEnabledForConversionUpdated(object? sender, bool isEnabledForConversion)
    {
        var video = sender as IConvertableVideo;
        var videoThumbViewModel = VideoList.FirstOrDefault(x => x.LinkedVideo == video);
        if (videoThumbViewModel == null)
            return;

        videoThumbViewModel.ShowAsSelectedForConversion = isEnabledForConversion;
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

    private void VideoOnConversionSettingsChanged(object? sender, EventArgs e)
    {
        var video = sender as IConvertableVideo;
        var videoThumbViewModel = VideoList.FirstOrDefault(x => x.LinkedVideo == video);
        if (videoThumbViewModel == null)
            return;

        videoThumbViewModel.HasConversionSettingsModified = video!.HasNonDefaultSettings;
    }

    [RelayCommand]
    private void ClearAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            var video = videoThumbViewModel.LinkedVideo;
            if (video != null)
            {
                _conversionManager.RemoveVideoFromPool(video);
            }
        }
        VideoList.Clear();
    }

    [RelayCommand]
    private void SelectAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            var video = videoThumbViewModel.LinkedVideo;
            if (video == null) 
                continue;
            
            video.IsEnabledForConversion = true;
            videoThumbViewModel.ShowAsSelectedForConversion = true;
        }
    }

    [RelayCommand]
    private void UnselectAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            if (videoThumbViewModel.LinkedVideo == null) 
                continue;
            
            videoThumbViewModel.LinkedVideo.IsEnabledForConversion = false;
            videoThumbViewModel.ShowAsSelectedForConversion = false;
        }
    }
    
    
}