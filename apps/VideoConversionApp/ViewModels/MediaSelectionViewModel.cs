using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.ViewModels;

public partial class MediaSelectionViewModel : MainViewModelPart
{

    private readonly IMediaInfoService _mediaInfoService;
    private readonly IStorageDialogProvider _storageDialogProvider;
    private readonly IMediaPreviewService _mediaPreviewService;
    private readonly IConversionManager _conversionManager;

    public ObservableCollection<VideoThumbViewModel> VideoList { get; private set; } =
        new ObservableCollection<VideoThumbViewModel>();
    
    public MediaSelectionViewModel(IServiceProvider serviceProvider, 
        IMediaInfoService mediaInfoService, 
        IStorageDialogProvider storageDialogProvider,
        IMediaPreviewService mediaPreviewService,
        IConversionManager conversionManager) : base(serviceProvider)
    {
        _mediaInfoService = mediaInfoService;
        _storageDialogProvider = storageDialogProvider;
        _mediaPreviewService = mediaPreviewService;
        _conversionManager = conversionManager;

        if (Design.IsDesignMode)
        {
            VideoList.Add(new VideoThumbViewModel());
            VideoList.Add(new VideoThumbViewModel());
        }
    }


    [RelayCommand]
    private async Task AddFiles()
    {
        var videoType = new FilePickerFileType("GoPro MAX .360");
        videoType.Patterns = [".360"];

        var selectedFiles = await _storageDialogProvider!.GetStorageProvider().OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = true,
            FileTypeFilter = [videoType]
        });

        var newThumbs = new List<(VideoThumbViewModel, MediaInfo)>();
        foreach (var selectedFile in selectedFiles)
        {
            var fullFilename = selectedFile!.TryGetLocalPath();
            if (VideoList.Any(v => v.FullFileName == fullFilename))
                continue;
            
            var mediaInfo = await _mediaInfoService.GetMediaInfoAsync(fullFilename!);
            var convertibleVideo = new ConvertibleVideoModel(mediaInfo);
            if (mediaInfo.IsValidVideo && mediaInfo.IsGoProMaxFormat)
                _conversionManager.AddToConversionCandidates(convertibleVideo);

            var thumbViewModel = new VideoThumbViewModel
            {
                FullFileName = fullFilename!,
                PreviewFileName = Path.GetFileName(fullFilename!),
                FileSize = mediaInfo.SizeBytes,
                VideoDateTime = mediaInfo.CreatedDateTime,
                VideoLength = mediaInfo.DurationSeconds,
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
            });
            
            thumbViewModel.OnSelectFileCommand = new RelayCommand<bool>((isChecked) =>
            {
                thumbViewModel.LinkedConvertibleVideoModel.IsEnabledForConversion = isChecked;
                thumbViewModel.ShowAsSelectedForConversion = isChecked;
            });
            newThumbs.Add((thumbViewModel, mediaInfo));
            VideoList.Add(thumbViewModel);
        }

        await Parallel.ForAsync(0, newThumbs.Count, async (i, token) =>
        {
            var thumbBytes = await _mediaPreviewService.GenerateThumbnailAsync(newThumbs[i].Item2);
            if (thumbBytes != null)
            {
                using var stream = new MemoryStream(thumbBytes);
                newThumbs[i].Item1.ThumbnailImage = new Bitmap(stream);
                newThumbs[i].Item1.HasLoadingThumbnail = false;
            }
        });

    }

    [RelayCommand]
    private void ClearAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            if (videoThumbViewModel.LinkedConvertibleVideoModel != null)
                _conversionManager.RemoveFromConversionCandidates(videoThumbViewModel.LinkedConvertibleVideoModel);
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