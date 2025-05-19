using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;
using VideoConversionApp.ViewModels.Components;

namespace VideoConversionApp.ViewModels;

public partial class MediaSelectionViewModel : ViewModelBase
{
    [AllowNull]
    private readonly IMediaInfoService _mediaInfoService;
    [AllowNull]
    private readonly IStorageDialogProvider _storageDialogProvider;
    [AllowNull]
    private readonly IMediaPreviewService _mediaPreviewService;

    public ObservableCollection<VideoThumbViewModel> VideoList { get; private set; } =
        new ObservableCollection<VideoThumbViewModel>();
    
    
    public MediaSelectionViewModel(IMediaInfoService mediaInfoService, 
        IStorageDialogProvider storageDialogProvider,
        IMediaPreviewService mediaPreviewService)
    {
        _mediaInfoService = mediaInfoService;
        _storageDialogProvider = storageDialogProvider;
        _mediaPreviewService = mediaPreviewService;

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
            var mediaInfo = await _mediaInfoService.GetMediaInfoAsync(fullFilename!);
            var thumbViewModel = new VideoThumbViewModel
            {
                FullFileName = fullFilename!,
                PreviewFileName = Path.GetFileName(fullFilename!),
                FileSize = mediaInfo.SizeBytes,
                VideoDateTime = mediaInfo.CreatedDateTime,
                VideoLength = mediaInfo.DurationSeconds,
                IsSelectedForConversion = false
            };
            thumbViewModel.OnCloseClickCommand = new RelayCommand(() =>
            {
                RemoveFile(thumbViewModel);
            });
            thumbViewModel.OnSelectFileCommand = new RelayCommand<bool>((isChecked) =>
            {
                Console.WriteLine("Checked: " + isChecked);
                // TODO select the file for conversion...
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

    private void RemoveFile(VideoThumbViewModel thumbViewModel)
    {
        // TODO also remove from conversion...
        VideoList.Remove(thumbViewModel);
    }

    [RelayCommand]
    private void ClearAllFiles()
    {
        // TODO also remove from conversion...
        VideoList.Clear();
    }

    [RelayCommand]
    private void SelectAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            videoThumbViewModel.IsSelectedForConversion = true;
        }
    }

    [RelayCommand]
    private void UnselectAllFiles()
    {
        foreach (var videoThumbViewModel in VideoList)
        {
            videoThumbViewModel.IsSelectedForConversion = false;
        }
    }
}