using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoConversionApp.Abstractions;
using VideoConversionApp.Models;

namespace VideoConversionApp.ViewModels;

public partial class RenderSettingsViewModel : ViewModelBase
{
    private readonly IVideoPoolManager _videoPoolManager;
    private readonly IVideoConverterService _converterService;
    private readonly IStorageDialogProvider _storageDialogProvider;
    private readonly IAppConfigService _appConfigService;

    [ObservableProperty]
    public partial string SelectedOutputDirectoryMethod { get; set; }
    [ObservableProperty]
    public partial string SelectedOutputDirectory { get; set; }
    [ObservableProperty]
    public partial string SelectedOutputDirectoryIssues { get; set; }
    [ObservableProperty]
    public partial string SelectedVideoCodecTab { get; set; }
    [ObservableProperty]
    public partial string SelectedAudioCodecTab { get; set; }
    [ObservableProperty]
    public partial bool IsOutputToSelectedDirSelected { get; set; } = true;
    [ObservableProperty]
    public partial bool IsOtherVideoCodecSelected { get; set; }
    [ObservableProperty]
    public partial bool IsOtherAudioCodecSelected { get; set; }
    [ObservableProperty]
    public partial string CustomVideoCodecName { get; set; }
    [ObservableProperty]
    public partial string CustomAudioCodecName { get; set; }
    [ObservableProperty]
    public partial string FilenamePattern { get; set; } = "%o";
    [ObservableProperty]
    public partial string FilenamePatternIssues { get; set; }
    [ObservableProperty]
    public partial string FilenamePreview { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<CodecEntry>? AudioCodecs { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<CodecEntry>? VideoCodecs { get; set; }

    public RenderSettingsViewModel(IVideoPoolManager videoPoolManager,
        IVideoConverterService converterService,
        IStorageDialogProvider storageDialogProvider,
        IAppConfigService appConfigService)
    {
        _videoPoolManager = videoPoolManager;
        _converterService = converterService;
        _storageDialogProvider = storageDialogProvider;
        _appConfigService = appConfigService;
    }

    partial void OnSelectedOutputDirectoryMethodChanged(string value)
    {
        IsOutputToSelectedDirSelected = value == "OutputToSelectedDir";
        _appConfigService.GetConfig().Conversion.OutputBesideOriginals = !IsOutputToSelectedDirSelected;
        //_videoPoolManager.GetConversionSettings().OutputBesideOriginals = !IsOutputToSelectedDirSelected;
    }

    partial void OnSelectedOutputDirectoryChanged(string value)
    {
        var exists = Directory.Exists(value);
        SelectedOutputDirectoryIssues = exists ? string.Empty : "Directory does not exist";
        _appConfigService.GetConfig().Conversion.OutputDirectory = value;
    }

    

    partial void OnSelectedVideoCodecTabChanged(string value)
    {
        IsOtherVideoCodecSelected = value == "VcOther";
        if (IsOtherVideoCodecSelected && VideoCodecs == null)
            RefreshCodecLists();
        
        //_videoPoolManager.GetConversionSettings().VideoCodecinFfmpeg = value switch
        _appConfigService.GetConfig().Conversion.CodecVideo = value switch
        {
            "VcProres" => "prores",
            "VcDnxhd" => "dnxhd",
            "VcCineform" => "cfhd",
            _ => CustomVideoCodecName
        };
    }

    partial void OnSelectedAudioCodecTabChanged(string value)
    {
        IsOtherAudioCodecSelected = value == "AcOther";
        if (IsOtherAudioCodecSelected && AudioCodecs == null)
            RefreshCodecLists();
        
        //_videoPoolManager.GetConversionSettings().AudioCodecinFfmpeg = value switch
        _appConfigService.GetConfig().Conversion.CodecAudio = value switch
        {
            "AcPcms16le" => "prores",
            "AcPcms32le" => "dnxhd",
            "AcNone" => "",
            _ => CustomVideoCodecName
        };
        _appConfigService.GetConfig().Conversion.OutputAudio = value != "AcNone";
        //_videoPoolManager.GetConversionSettings().OutputAudio = false;
    }

    partial void OnCustomVideoCodecNameChanged(string value)
    {
        if (IsOtherVideoCodecSelected)
            _appConfigService.GetConfig().Conversion.CodecVideo = value;
            //_videoPoolManager.GetConversionSettings().VideoCodecinFfmpeg = value;
    }

    partial void OnCustomAudioCodecNameChanged(string value)
    {
        if (IsOtherAudioCodecSelected)
            _appConfigService.GetConfig().Conversion.CodecAudio = value;
            //_videoPoolManager.GetConversionSettings().AudioCodecinFfmpeg = value;
    }

    partial void OnFilenamePatternChanged(string value)
    {
        var dummyVideo = _videoPoolManager.GetDummyVideo();
        FilenamePreview = _converterService.GetFilenameFromPattern(dummyVideo, value);
        
        _appConfigService.GetConfig().Conversion.OutputFilenamePattern = value;
        //_videoPoolManager.GetConversionSettings().OutputFilenamePattern = value;

        FilenamePatternIssues = string.IsNullOrWhiteSpace(FilenamePattern) ? "Filename pattern is empty" : "";
    }

    public void RefreshCodecLists()
    {
        VideoCodecs = new ObservableCollection<CodecEntry>(_converterService.GetAvailableVideoCodecs().Where(x => x.EncodingSupported));
        AudioCodecs = new ObservableCollection<CodecEntry>(_converterService.GetAvailableAudioCodecs().Where(x => x.EncodingSupported));
    }

    [RelayCommand]
    private async Task BrowseOutputDirectory()
    {
        //var appSettings = _appSettingsService.GetSettings();
        
        var firstInputVideo = _videoPoolManager.VideoPool.FirstOrDefault();
        var suggestedStartLocation = !string.IsNullOrEmpty(SelectedOutputDirectory) && Directory.Exists(SelectedOutputDirectory)
            ? SelectedOutputDirectory
            : firstInputVideo != null
                ? Path.GetDirectoryName(firstInputVideo.InputVideoInfo.Filename)
                : null;

        var storageProvider = _storageDialogProvider!.GetStorageProvider();
        var selectedDirectory = await storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            SuggestedStartLocation = suggestedStartLocation != null
                ? await storageProvider.TryGetFolderFromPathAsync(new Uri(suggestedStartLocation))
                : await storageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Videos),
            Title = "Select Output Directory"
        });
        
        if (selectedDirectory.Count > 0)
            SelectedOutputDirectory = selectedDirectory[0].TryGetLocalPath()!;
        

    }
}